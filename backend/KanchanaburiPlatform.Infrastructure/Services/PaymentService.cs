using Application.DTOs;
using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Infrastructure.Services;

public sealed class PaymentService(IUnitOfWork unit, IOrderExpiryService orderExpiryService, IConfiguration configuration, ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<ServiceResult<CreatePaymentIntentResponse>> CreateIntentAsync(Guid orderId, string? userId)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(orderId);
        if (order is null || order.UserId != userId) return ServiceResult<CreatePaymentIntentResponse>.Failure(ServiceStatus.NotFound);
        if (await orderExpiryService.ExpireIfNeededAsync(orderId)) return Invalid<CreatePaymentIntentResponse>("Payment time has expired");
        order = await unit.Repository<Order>().GetByIdAsync(orderId);
        if (order is null || order.OrderStatus == "Expired") return Invalid<CreatePaymentIntentResponse>("Payment time has expired");
        if (order.PaymentStatus == "Paid") return Invalid<CreatePaymentIntentResponse>("Order is already paid");
        var intent = await CreateStripeIntentAsync(order.TotalAmount, new() { ["orderId"] = order.OrderId.ToString() });
        if (intent is null) return Unavailable<CreatePaymentIntentResponse>();
        unit.Repository<Payment>().Add(NewPayment(order, intent.Id)); await unit.Complete();
        return ServiceResult<CreatePaymentIntentResponse>.Success(new(intent.ClientSecret, intent.Id));
    }

    public async Task<ServiceResult<bool>> ProcessWebhookAsync(string json, string? signature)
    {
        var secret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret)) { logger.LogError("Stripe:WebhookSecret is not configured."); return Unavailable<bool>("Stripe:WebhookSecret is not configured"); }
        if (string.IsNullOrEmpty(signature)) { logger.LogWarning("Stripe-Signature header missing"); return Invalid<bool>("Stripe-Signature header missing"); }
        Event stripeEvent;
        try { stripeEvent = EventUtility.ConstructEvent(json, signature, secret, throwOnApiVersionMismatch: false); }
        catch (StripeException ex) { logger.LogError(ex, "Stripe webhook verification failed"); return Invalid<bool>($"Webhook Signature Failed: {ex.Message}"); }
        catch (Exception ex) { logger.LogError(ex, "Stripe webhook processing error"); return Invalid<bool>($"Webhook Error: {ex.Message}"); }

        var intent = stripeEvent.Data.Object as PaymentIntent;
        if (intent is null) return ServiceResult<bool>.Success(true);
        var payment = (await unit.Repository<Payment>().ListAllAsync()).FirstOrDefault(x => x.TransactionRef == intent.Id);
        if (stripeEvent.Type == "payment_intent.payment_failed")
        {
            if (payment?.PaymentStatus == "Pending") { payment.PaymentStatus = "Failed"; unit.Repository<Payment>().Update(payment); await unit.Complete(); }
            return ServiceResult<bool>.Success(true);
        }
        if (stripeEvent.Type != "payment_intent.succeeded" || payment is null || payment.PaymentStatus == "Paid") return ServiceResult<bool>.Success(true);
        var order = await unit.Repository<Order>().GetByIdAsync(payment.OrderId); if (order is null) return ServiceResult<bool>.Success(true);
        MarkPaid(payment, order); unit.Repository<Payment>().Update(payment); unit.Repository<Order>().Update(order); await unit.Complete(); return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> SyncPaymentAsync(Guid orderId, string? userId)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(orderId); if (order is null || order.UserId != userId) return ServiceResult<bool>.Failure(ServiceStatus.NotFound); if (order.PaymentStatus == "Paid") return ServiceResult<bool>.Success(true);
        var payment = (await unit.Repository<Payment>().ListAllAsync()).Where(x => x.OrderId == orderId && x.PaymentMethod == "Stripe").OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        if (payment is null || string.IsNullOrWhiteSpace(payment.TransactionRef)) return Invalid<bool>("Stripe payment was not found");
        var intent = await GetStripeIntentAsync(payment.TransactionRef); if (intent is null) return Unavailable<bool>(); if (intent.Status != "succeeded") return Invalid<bool>("Payment is not complete");
        MarkPaid(payment, order); unit.Repository<Payment>().Update(payment); unit.Repository<Order>().Update(order); return await unit.Complete() ? ServiceResult<bool>.Success(true) : Invalid<bool>("Unable to update payment status");
    }

    public async Task<ServiceResult<BatchOrderSummaryDto>> GetOrdersSummaryAsync(string orderIds, string? userId)
    {
        if (string.IsNullOrWhiteSpace(orderIds)) return Invalid<BatchOrderSummaryDto>("orderIds is required"); var ids = ParseIds(orderIds); if (ids.Count == 0) return Invalid<BatchOrderSummaryDto>("Invalid orderIds");
        var orders = (await unit.Repository<Order>().ListAllAsync()).Where(x => ids.Contains(x.OrderId) && x.UserId == userId).ToList(); if (orders.Count == 0) return ServiceResult<BatchOrderSummaryDto>.Failure(ServiceStatus.NotFound);
        var shops = (await unit.Repository<Shop>().ListAllAsync()).Where(x => orders.Select(o => o.ShopId).Contains(x.ShopId)).ToDictionary(x => x.ShopId);
        var items = orders.Select(o => new OrderSummaryItemDto { OrderId = o.OrderId, OrderNumber = o.OrderNumber, ShopId = o.ShopId, ShopName = shops.TryGetValue(o.ShopId, out var s) ? s.ShopName : null, Subtotal = o.Subtotal, ShippingFee = o.ShippingFee, TotalAmount = o.TotalAmount, PaymentStatus = o.PaymentStatus }).ToList();
        return ServiceResult<BatchOrderSummaryDto>.Success(new() { Orders = items, TotalAmount = items.Sum(x => x.TotalAmount), IsAllPaid = items.All(x => x.PaymentStatus == "Paid") });
    }

    public async Task<ServiceResult<CreatePaymentIntentResponse>> CreateBatchIntentAsync(BatchPaymentRequestDto dto, string? userId)
    {
        if (dto.OrderIds is null || dto.OrderIds.Count == 0) return Invalid<CreatePaymentIntentResponse>("No order IDs provided");
        var orders = (await unit.Repository<Order>().ListAllAsync()).Where(x => dto.OrderIds.Contains(x.OrderId) && x.UserId == userId).ToList(); if (orders.Count != dto.OrderIds.Distinct().Count()) return Invalid<CreatePaymentIntentResponse>("One or more orders were not found");
        foreach (var order in orders) await orderExpiryService.ExpireIfNeededAsync(order.OrderId);
        orders = (await unit.Repository<Order>().ListAllAsync()).Where(x => dto.OrderIds.Contains(x.OrderId) && x.UserId == userId).ToList();
        var unpaid = orders.Where(x => x.PaymentStatus != "Paid" && x.OrderStatus != "Expired").ToList(); if (unpaid.Count == 0) return Invalid<CreatePaymentIntentResponse>("All orders are already paid or have expired");
        var intent = await CreateStripeIntentAsync(unpaid.Sum(x => x.TotalAmount), new() { ["orderIds"] = string.Join(',', unpaid.Select(x => x.OrderId)) }); if (intent is null) return Unavailable<CreatePaymentIntentResponse>();
        foreach (var order in unpaid) unit.Repository<Payment>().Add(NewPayment(order, intent.Id)); await unit.Complete(); return ServiceResult<CreatePaymentIntentResponse>.Success(new(intent.ClientSecret, intent.Id));
    }

    public async Task<ServiceResult<bool>> SyncBatchPaymentAsync(BatchPaymentRequestDto dto, string? userId)
    {
        if (dto.OrderIds is null || dto.OrderIds.Count == 0) return Invalid<bool>("No order IDs provided"); var orders = (await unit.Repository<Order>().ListAllAsync()).Where(x => dto.OrderIds.Contains(x.OrderId) && x.UserId == userId).ToList(); if (orders.Count == 0) return ServiceResult<bool>.Failure(ServiceStatus.NotFound, "Orders not found"); var unpaid = orders.Where(x => x.PaymentStatus != "Paid").ToList(); if (unpaid.Count == 0) return ServiceResult<bool>.Success(true);
        var payments = (await unit.Repository<Payment>().ListAllAsync()).Where(x => unpaid.Select(o => o.OrderId).Contains(x.OrderId) && x.PaymentMethod == "Stripe").OrderByDescending(x => x.CreatedAt).ToList(); var reference = payments.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.TransactionRef)); if (reference is null) return Invalid<bool>("Stripe payment transaction reference not found"); var intent = await GetStripeIntentAsync(reference.TransactionRef!); if (intent is null) return Unavailable<bool>(); if (intent.Status != "succeeded") return Invalid<bool>("Payment is not complete");
        var now = DateTime.UtcNow; foreach (var order in unpaid) { order.PaymentStatus = "Paid"; unit.Repository<Order>().Update(order); } foreach (var payment in (await unit.Repository<Payment>().ListAllAsync()).Where(x => x.TransactionRef == reference.TransactionRef)) { payment.PaymentStatus = "Paid"; payment.PaidAt = now; unit.Repository<Payment>().Update(payment); } return await unit.Complete() ? ServiceResult<bool>.Success(true) : Invalid<bool>("Unable to update payment status");
    }

    private async Task<PaymentIntent?> CreateStripeIntentAsync(decimal amount, Dictionary<string, string> metadata) { var key = ConfigureStripe(); if (key is null) return null; return await new PaymentIntentService().CreateAsync(new PaymentIntentCreateOptions { Amount = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero), Currency = "thb", AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }, Metadata = metadata }); }
    private async Task<PaymentIntent?> GetStripeIntentAsync(string transactionRef) => ConfigureStripe() is null ? null : await new PaymentIntentService().GetAsync(transactionRef);
    private string? ConfigureStripe() { var key = configuration["Stripe:SecretKey"]; if (string.IsNullOrWhiteSpace(key)) return null; StripeConfiguration.ApiKey = key; return key; }
    private static List<Guid> ParseIds(string value) => value.Split(',').Select(x => Guid.TryParse(x.Trim(), out var id) ? id : Guid.Empty).Where(x => x != Guid.Empty).ToList();
    private static Payment NewPayment(Order order, string transactionRef) => new() { PaymentId = Guid.NewGuid(), OrderId = order.OrderId, PaymentMethod = "Stripe", TransactionRef = transactionRef, Amount = order.TotalAmount, PaymentStatus = "Pending", CreatedAt = DateTime.UtcNow };
    private static void MarkPaid(Payment payment, Order order) { payment.PaymentStatus = "Paid"; payment.PaidAt = DateTime.UtcNow; order.PaymentStatus = "Paid"; }
    private static ServiceResult<T> Invalid<T>(string? error = null) => ServiceResult<T>.Failure(ServiceStatus.Invalid, error);
    private static ServiceResult<T> Unavailable<T>(string? error = null) => ServiceResult<T>.Failure(ServiceStatus.Failed, error ?? "Stripe:SecretKey is not configured");
}
