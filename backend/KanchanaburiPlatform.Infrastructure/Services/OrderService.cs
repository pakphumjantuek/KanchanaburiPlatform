using Application.DTOs;
using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class OrderService(IUnitOfWork unit, IPaymentSlipService paymentSlipService, IWebHostEnvironment environment) : IOrderService
{
    public async Task<ServiceResult<IReadOnlyList<OrderDto>>> CheckoutAsync(string userId, CheckoutDto dto)
    {
        var cart = (await unit.Repository<Cart>().ListAllAsync()).FirstOrDefault(x => x.UserId == userId);
        if (cart is null) return Fail<IReadOnlyList<OrderDto>>("Cart not found");
        var cartItems = (await unit.Repository<CartItem>().ListAllAsync()).Where(x => x.CartId == cart.CartId).ToList();
        if (cartItems.Count == 0) return Fail<IReadOnlyList<OrderDto>>("Cart is empty");
        if (string.IsNullOrWhiteSpace(dto.ReceiverName) || string.IsNullOrWhiteSpace(dto.ReceiverPhone) || string.IsNullOrWhiteSpace(dto.ShippingAddress)) return Fail<IReadOnlyList<OrderDto>>("Receiver name, phone, and shipping address are required for delivery");

        var products = new Dictionary<Guid, Product>();
        foreach (var item in cartItems) { var p = await unit.Repository<Product>().GetByIdAsync(item.ProductId); if (p is null || p.Status != "Active") return Fail<IReadOnlyList<OrderDto>>("Cart contains an unavailable product"); products[item.ProductId] = p; }
        foreach (var item in cartItems) if (products[item.ProductId].Quantity < item.Quantity) return Fail<IReadOnlyList<OrderDto>>($"Insufficient stock for product: {products[item.ProductId].ProductName}");
        var now = DateTime.UtcNow;
        var activeDeals = (await unit.Repository<ProductDeal>().ListAsync(deal => deal.Status == "Active" && deal.StartsAt <= now && deal.EndsAt > now && deal.UsedQuantity < deal.TotalQuantity))
            .Where(deal => products.ContainsKey(deal.ProductId)).GroupBy(deal => deal.ProductId).ToDictionary(group => group.Key, group => group.OrderBy(deal => deal.EndsAt).First());
        var groups = cartItems.GroupBy(x => products[x.ProductId].ShopId).ToList();
        var shops = (await unit.Repository<Shop>().ListAllAsync()).ToDictionary(x => x.ShopId);
        if (groups.Any(g => !shops.TryGetValue(g.Key, out var s) || s.Status != "Active")) return Fail<IReadOnlyList<OrderDto>>("Cart contains a product from an unavailable shop");

        var orders = new List<Order>(); await unit.BeginTransactionAsync();
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"); var index = 0;
            foreach (var group in groups)
            {
                index++; var groupItems = group.ToList(); var subtotal = groupItems.Sum(x => LineTotal(products[x.ProductId], activeDeals.TryGetValue(x.ProductId, out var deal) ? deal : null, x.Quantity));
                var order = new Order { OrderId = Guid.NewGuid(), UserId = userId, ShopId = group.Key, OrderNumber = $"ORD-{timestamp}-{Random.Shared.Next(1000, 9999)}{(groups.Count > 1 ? $"-{index}" : "")}", Subtotal = subtotal, ShippingFee = dto.ShippingFee > 0 ? dto.ShippingFee : 50m, ShippingMethod = "Delivery", ReceiverName = dto.ReceiverName, ReceiverPhone = dto.ReceiverPhone, ShippingAddress = dto.ShippingAddress, CreatedAt = DateTime.UtcNow, PaymentExpiresAt = DateTime.UtcNow.AddMinutes(30) };
                order.TotalAmount = order.Subtotal + order.ShippingFee; unit.Repository<Order>().Add(order);
                foreach (var item in groupItems) { var p = products[item.ProductId]; var deal = activeDeals.TryGetValue(item.ProductId, out var value) ? value : null; var dealQuantity = DealQuantity(p, deal, item.Quantity); var normalQuantity = item.Quantity - dealQuantity; p.Quantity -= item.Quantity; p.UpdatedAt = DateTime.UtcNow; unit.Repository<Product>().Update(p); if (dealQuantity > 0 && deal is not null) { var unitPrice = PriceFor(p, deal); deal.UsedQuantity += dealQuantity; deal.UpdatedAt = DateTime.UtcNow; unit.Repository<ProductDeal>().Update(deal); unit.Repository<OrderItem>().Add(new OrderItem { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = item.ProductId, ProductDealId = deal.ProductDealId, ProductName = p.ProductName, Quantity = dealQuantity, UnitPrice = unitPrice, TotalPrice = unitPrice * dealQuantity }); } if (normalQuantity > 0) unit.Repository<OrderItem>().Add(new OrderItem { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = item.ProductId, ProductName = p.ProductName, Quantity = normalQuantity, UnitPrice = p.Price, TotalPrice = p.Price * normalQuantity }); unit.Repository<CartItem>().Remove(item); }
                orders.Add(order);
            }
            if (!await unit.Complete()) { await unit.RollbackTransactionAsync(); return Fail<IReadOnlyList<OrderDto>>("Checkout failed"); }
            await unit.CommitTransactionAsync();
            return ServiceResult<IReadOnlyList<OrderDto>>.Success(orders.Select(o => WithShop(ToDto(o), shops[o.ShopId])).ToList());
        }
        catch (DbUpdateConcurrencyException)
        {
            await unit.RollbackTransactionAsync();
            return ServiceResult<IReadOnlyList<OrderDto>>.Failure(
                ServiceStatus.Conflict,
                "สินค้าบางรายการหมด กรุณาตรวจสอบตะกร้าก่อนสั่งซื้ออีกครั้ง");
        }
        catch { await unit.RollbackTransactionAsync(); throw; }
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(string userId)
    {
        var orders = (await unit.Repository<Order>().ListAsync(x => x.UserId == userId)).OrderByDescending(x => x.CreatedAt).ToList();
        return await MapOrdersAsync(orders, includeShipment: true, includeImages: true);
    }

    public async Task<ServiceResult<OrderDetailDto>> GetOrderAsync(Guid orderId, string? userId, bool isAdmin)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(orderId); if (order is null) return ServiceResult<OrderDetailDto>.Failure(ServiceStatus.NotFound);
        var shop = await unit.Repository<Shop>().GetByIdAsync(order.ShopId); if (order.UserId != userId && !isAdmin && shop?.OwnerUserId != userId) return ServiceResult<OrderDetailDto>.Failure(ServiceStatus.Forbidden);
        var dto = new OrderDetailDto(); Copy(ToDto(order), dto); if (shop is not null) WithShop(dto, shop);
        dto.Items = await MapItemsAsync(orderId, true); var shipment = await unit.Repository<Shipment>().FirstOrDefaultAsync(x => x.OrderId == orderId); dto.Shipment = ToDto(shipment);
        return ServiceResult<OrderDetailDto>.Success(dto);
    }

    public async Task<ServiceResult<IReadOnlyList<OrderDto>>> GetMyShopOrdersAsync(string userId, string? status, string? search, DateTime? startDate, DateTime? endDate)
    {
        var shop = await unit.Repository<Shop>().FirstOrDefaultAsync(
            x => x.OwnerUserId == userId
        );

        if (shop is null)
        {
            return ServiceResult<IReadOnlyList<OrderDto>>.Failure(
                ServiceStatus.NotFound,
                "Shop not found"
            );
        }

        var requestedStatus = string.IsNullOrWhiteSpace(status) || status == "all"
            ? null
            : status;
        var searchTerm = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim();
        var start = startDate?.Date;
        var end = endDate?.Date.AddDays(1);

        var orders = await unit.Repository<Order>().ListAsync(order =>
            order.ShopId == shop.ShopId &&
            order.PaymentStatus == "Paid" &&
            (requestedStatus == null ||
                requestedStatus == "paid" ||
                order.OrderStatus == requestedStatus) &&
            (searchTerm == null || order.OrderNumber.Contains(searchTerm)) &&
            (!start.HasValue || order.CreatedAt >= start) &&
            (!end.HasValue || order.CreatedAt < end)
        );

        var sortedOrders = orders
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        var result = await MapOrdersAsync(
            sortedOrders,
            includeShipment: false,
            includeImages: true
        );

        return ServiceResult<IReadOnlyList<OrderDto>>.Success(result);
    }

    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto, string? userId, bool isAdmin)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(id);
        if (order is null)
        {
            return ServiceResult<bool>.Failure(ServiceStatus.NotFound);
        }

        var shop = await unit.Repository<Shop>().GetByIdAsync(order.ShopId);
        var isMerchant = shop?.OwnerUserId == userId;
        var isOrderOwner = order.UserId == userId;

        if (!isMerchant && !isAdmin && !isOrderOwner)
        {
            return ServiceResult<bool>.Failure(ServiceStatus.Forbidden);
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var allowedStatuses = new[]
            {
                "Pending",
                "Confirmed",
                "Processing",
                "Shipped",
                "Completed",
                "Cancelled",
            };

            if (!allowedStatuses.Contains(dto.Status))
            {
                return Fail<bool>("Invalid order status");
            }

            if (isMerchant && !isAdmin)
            {
                var canChangeStatus =
                    (order.OrderStatus == "Pending" &&
                        (dto.Status == "Confirmed" || dto.Status == "Cancelled")) ||
                    (order.OrderStatus == "Confirmed" && dto.Status == "Processing");

                if (!canChangeStatus)
                {
                    return Fail<bool>("Invalid merchant order status transition");
                }
            }

            if (dto.Status is not "Cancelled" and not "Pending" && order.PaymentStatus != "Paid")
            {
                return Fail<bool>("Order must be paid before processing");
            }

            if (dto.Status == "Cancelled" &&
                order.OrderStatus == "Pending" &&
                order.PaymentStatus == "Pending")
            {
                await RestoreReservedStockAsync(order);
            }

            order.OrderStatus = dto.Status;
        }

        if (!string.IsNullOrWhiteSpace(dto.PaymentStatus))
        {
            if (!isAdmin)
            {
                return ServiceResult<bool>.Failure(ServiceStatus.Forbidden);
            }

            var allowedPaymentStatuses = new[]
            {
                "Pending",
                "PendingVerification",
                "Paid",
                "Failed",
            };

            if (!allowedPaymentStatuses.Contains(dto.PaymentStatus))
            {
                return Fail<bool>("Invalid payment status");
            }

            order.PaymentStatus = dto.PaymentStatus;
        }

        if (!string.IsNullOrWhiteSpace(dto.SlipImageUrl))
        {
            order.SlipImageUrl = dto.SlipImageUrl;
        }

        if (dto.SlipUploadedAt.HasValue)
        {
            order.SlipUploadedAt = dto.SlipUploadedAt;
        }

        unit.Repository<Order>().Update(order);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Fail<bool>();
    }

    public async Task<ServiceResult<OrderDto>> UploadSlipAsync(Guid id, UploadPaymentSlipFormDto dto, string? userId, bool isAdmin, CancellationToken ct = default)
    {
        var result = await paymentSlipService.UploadAsync(id, dto, userId, isAdmin, ct); return result.Status == ServiceStatus.Success ? ServiceResult<OrderDto>.Success(ToDto(result.Data!)) : ServiceResult<OrderDto>.Failure(result.Status, result.Error);
    }

    public async Task<ServiceResult<ShipmentDto>> UpdateShipmentAsync(Guid id, UpdateShipmentDto dto, string? userId, bool isAdmin)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(id); if (order is null) return ServiceResult<ShipmentDto>.Failure(ServiceStatus.NotFound); var shop = await unit.Repository<Shop>().GetByIdAsync(order.ShopId);
        if (shop is null || (!isAdmin && shop.OwnerUserId != userId)) return ServiceResult<ShipmentDto>.Failure(ServiceStatus.Forbidden); if (order.PaymentStatus != "Paid") return Fail<ShipmentDto>("Order must be paid before shipping"); if (order.OrderStatus != "Processing") return Fail<ShipmentDto>("Order must be prepared before shipping"); if (string.IsNullOrWhiteSpace(dto.ShippingProvider) || string.IsNullOrWhiteSpace(dto.TrackingNumber)) return Fail<ShipmentDto>("Shipping provider and tracking number are required");
        var shipment = (await unit.Repository<Shipment>().ListAllAsync()).FirstOrDefault(x => x.OrderId == id); if (shipment is null) { shipment = new Shipment { ShipmentId = Guid.NewGuid(), OrderId = id }; unit.Repository<Shipment>().Add(shipment); } shipment.ShippingProvider = dto.ShippingProvider; shipment.TrackingNumber = dto.TrackingNumber; shipment.ShippingStatus = "Shipped"; shipment.ShippedAt = DateTime.UtcNow; order.OrderStatus = "Shipped"; unit.Repository<Order>().Update(order);
        return await unit.Complete() ? ServiceResult<ShipmentDto>.Success(ToDto(shipment)!) : Fail<ShipmentDto>();
    }

    public async Task<ServiceResult<bool>> UpdateShipmentStatusAsync(Guid id, UpdateShipmentStatusDto dto, string? userId, bool isAdmin)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(id);
        if (order is null)
        {
            return ServiceResult<bool>.Failure(ServiceStatus.NotFound);
        }

        if (!isAdmin && order.UserId != userId)
        {
            return ServiceResult<bool>.Failure(ServiceStatus.Forbidden);
        }

        if (dto.Status != "Completed")
        {
            return Fail<bool>("Only completed status is allowed");
        }

        if (order.PaymentStatus != "Paid" || order.OrderStatus != "Shipped")
        {
            return Fail<bool>("Order must be shipped before confirming receipt");
        }

        var shipment = await unit.Repository<Shipment>().FirstOrDefaultAsync(
            value => value.OrderId == id
        );

        if (shipment is null)
        {
            return Fail<bool>("Shipment not found");
        }

        shipment.ShippingStatus = "Delivered";
        shipment.DeliveredAt = DateTime.UtcNow;
        order.OrderStatus = "Completed";

        unit.Repository<Shipment>().Update(shipment);
        unit.Repository<Order>().Update(order);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Fail<bool>();
    }

    public async Task<IReadOnlyList<AdminPendingSlipGroupDto>> GetPendingSlipsAsync()
    {
        var orders = (await unit.Repository<Order>().ListAsync(x => x.PaymentStatus == "PendingVerification" || (!string.IsNullOrEmpty(x.SlipImageUrl) && x.PaymentStatus != "Paid"))).OrderByDescending(x => x.SlipUploadedAt ?? x.CreatedAt).ToList();
        var mapped = await MapOrdersAsync(orders, false, false); return orders.GroupBy(x => string.IsNullOrWhiteSpace(x.SlipImageUrl) ? x.OrderId.ToString() : x.SlipImageUrl).Select(g => new AdminPendingSlipGroupDto { SlipImageUrl = g.First().SlipImageUrl ?? string.Empty, SlipUploadedAt = g.First().SlipUploadedAt, TotalGroupAmount = g.Sum(x => x.TotalAmount), BuyerName = g.First().ReceiverName, BuyerPhone = g.First().ReceiverPhone, Orders = mapped.Where(x => g.Any(o => o.OrderId == x.OrderId)).ToList() }).ToList();
    }

    public async Task<ServiceResult<bool>> VerifySlipAsync(VerifySlipDto dto)
    {
        if (dto.OrderIds is null || dto.OrderIds.Count == 0) return Fail<bool>("OrderIds is required"); var orders = await unit.Repository<Order>().ListAsync(x => dto.OrderIds.Contains(x.OrderId)); if (orders.Count == 0) return ServiceResult<bool>.Failure(ServiceStatus.NotFound, "Orders not found"); var approve = dto.Action.Equals("Approve", StringComparison.OrdinalIgnoreCase); foreach (var o in orders) { o.PaymentStatus = approve ? "Paid" : "Failed"; unit.Repository<Order>().Update(o); } return await unit.Complete() ? ServiceResult<bool>.Success(true) : Fail<bool>("Unable to update order verification status");
    }

    public async Task<IReadOnlyList<AdminMerchantPayoutGroupDto>> GetPayoutsAsync()
    {
        var orders = (await unit.Repository<Order>().ListAsync(x => x.PaymentStatus == "Paid" && x.PayoutStatus != "Settled")).OrderByDescending(x => x.CreatedAt).ToList(); var mapped = await MapOrdersAsync(orders, false, false); var shops = (await unit.Repository<Shop>().ListAsync(x => orders.Select(o => o.ShopId).Contains(x.ShopId))).ToDictionary(x => x.ShopId);
        return orders.GroupBy(x => x.ShopId).Where(g => shops.ContainsKey(g.Key)).Select(g => { var s = shops[g.Key]; return new AdminMerchantPayoutGroupDto { ShopId = s.ShopId, ShopName = s.ShopName, ShopLogoUrl = s.CoverImageUrl, BankName = s.BankName, BankAccountName = s.BankAccountName, BankAccountNumber = s.BankAccountNumber, PromptPay = s.PromptPay, TotalSalesAmount = g.Sum(x => x.TotalAmount), PaidOrdersCount = g.Count(), Orders = mapped.Where(x => x.ShopId == s.ShopId).ToList() }; }).OrderByDescending(x => x.TotalSalesAmount).ToList();
    }

    public async Task<ServiceResult<MerchantPayoutRecordDto>> ConfirmPayoutAsync(AdminConfirmPayoutFormDto dto)
    {
        var ids = (dto.OrderIds ?? "").Split(',').Select(x => Guid.TryParse(x.Trim(), out var id) ? id : Guid.Empty).Where(x => x != Guid.Empty).ToList(); if (ids.Count == 0) return Fail<MerchantPayoutRecordDto>("OrderIds is required"); var shop = await unit.Repository<Shop>().GetByIdAsync(dto.ShopId); if (shop is null) return ServiceResult<MerchantPayoutRecordDto>.Failure(ServiceStatus.NotFound, "Shop not found"); var orders = await unit.Repository<Order>().ListAsync(x => x.ShopId == dto.ShopId && ids.Contains(x.OrderId) && x.PaymentStatus == "Paid"); if (orders.Count == 0) return Fail<MerchantPayoutRecordDto>("No valid paid orders found for this payout");
        string? url = null; if (dto.SlipFile?.Length > 0 && new[] { "image/jpeg", "image/png", "image/webp" }.Contains(dto.SlipFile.ContentType.ToLowerInvariant())) { var ext = Path.GetExtension(dto.SlipFile.FileName).ToLowerInvariant(); var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "payouts"); Directory.CreateDirectory(folder); var name = $"{Guid.NewGuid():N}{ext}"; await using var stream = File.Create(Path.Combine(folder, name)); await dto.SlipFile.CopyToAsync(stream); url = $"/uploads/payouts/{name}"; }
        var payout = new MerchantPayout { PayoutId = Guid.NewGuid(), ShopId = dto.ShopId, TotalAmount = orders.Sum(x => x.TotalAmount), SlipImageUrl = url, TransactionRef = dto.TransactionRef, Note = dto.Note, Status = "Completed", CreatedAt = DateTime.UtcNow }; unit.Repository<MerchantPayout>().Add(payout); foreach (var o in orders) { o.PayoutStatus = "Settled"; o.MerchantPayoutId = payout.PayoutId; unit.Repository<Order>().Update(o); } if (!await unit.Complete()) return Fail<MerchantPayoutRecordDto>("Failed to confirm merchant payout"); return ServiceResult<MerchantPayoutRecordDto>.Success(ToPayoutDto(payout, shop, orders));
    }

    public async Task<IReadOnlyList<MerchantPayoutRecordDto>> GetPayoutHistoryAsync()
    {
        var payouts = (await unit.Repository<MerchantPayout>().ListAllAsync()).OrderByDescending(x => x.CreatedAt).ToList(); var shops = (await unit.Repository<Shop>().ListAllAsync()).ToDictionary(x => x.ShopId); var orders = await unit.Repository<Order>().ListAsync(x => x.MerchantPayoutId.HasValue); return payouts.Select(p => ToPayoutDto(p, shops.TryGetValue(p.ShopId, out var s) ? s : null, orders.Where(x => x.MerchantPayoutId == p.PayoutId))).ToList();
    }

    public async Task<ServiceResult<MerchantPayoutSummaryDto>> GetMerchantPayoutsAsync(string userId)
    {
        var shop = await unit.Repository<Shop>().FirstOrDefaultAsync(x => x.OwnerUserId == userId); if (shop is null) return ServiceResult<MerchantPayoutSummaryDto>.Failure(ServiceStatus.NotFound, "Shop not found"); var orders = await unit.Repository<Order>().ListAsync(x => x.ShopId == shop.ShopId); var payouts = (await unit.Repository<MerchantPayout>().ListAsync(x => x.ShopId == shop.ShopId)).OrderByDescending(x => x.CreatedAt).ToList(); return ServiceResult<MerchantPayoutSummaryDto>.Success(new MerchantPayoutSummaryDto { PendingPayoutAmount = orders.Where(x => x.PaymentStatus == "Paid" && x.PayoutStatus != "Settled").Sum(x => x.TotalAmount), TotalPaidOutAmount = payouts.Sum(x => x.TotalAmount), History = payouts.Select(p => ToPayoutDto(p, shop, orders.Where(x => x.MerchantPayoutId == p.PayoutId))).ToList() });
    }

    private async Task RestoreReservedStockAsync(Order order)
    {
        var reservedItems = await unit.Repository<OrderItem>().ListAsync(
            item => item.OrderId == order.OrderId
        );

        foreach (var item in reservedItems)
        {
            var product = await unit.Repository<Product>().GetByIdAsync(item.ProductId);
            if (product is not null)
            {
                product.Quantity += item.Quantity;
                unit.Repository<Product>().Update(product);
            }

            if (!item.ProductDealId.HasValue)
            {
                continue;
            }

            var deal = await unit.Repository<ProductDeal>().GetByIdAsync(
                item.ProductDealId.Value
            );

            if (deal is not null)
            {
                deal.UsedQuantity = Math.Max(0, deal.UsedQuantity - item.Quantity);
                unit.Repository<ProductDeal>().Update(deal);
            }
        }
    }

    private async Task<List<OrderDto>> MapOrdersAsync(List<Order> orders, bool includeShipment, bool includeImages)
    { var ids = orders.Select(x => x.OrderId).ToHashSet(); var shops = (await unit.Repository<Shop>().ListAsync(x => orders.Select(o => o.ShopId).Contains(x.ShopId))).ToDictionary(x => x.ShopId); var items = await unit.Repository<OrderItem>().ListAsync(x => ids.Contains(x.OrderId)); var products = includeImages ? (await unit.Repository<Product>().ListAsync(x => items.Select(i => i.ProductId).Contains(x.ProductId))).ToDictionary(x => x.ProductId) : []; var shipments = includeShipment ? (await unit.Repository<Shipment>().ListAsync(x => ids.Contains(x.OrderId))).ToDictionary(x => x.OrderId) : []; return orders.Select(o => { var d = ToDto(o); if (shops.TryGetValue(o.ShopId, out var s)) WithShop(d, s); d.Items = items.Where(i => i.OrderId == o.OrderId).Select(i => ToDto(i, products.TryGetValue(i.ProductId, out var p) ? p.ImageUrl : null)).ToList(); if (shipments.TryGetValue(o.OrderId, out var sh)) d.Shipment = ToDto(sh); return d; }).ToList(); }
    private async Task<List<OrderItemDto>> MapItemsAsync(Guid orderId, bool images) { var items = await unit.Repository<OrderItem>().ListAsync(x => x.OrderId == orderId); var products = images ? (await unit.Repository<Product>().ListAsync(x => items.Select(i => i.ProductId).Contains(x.ProductId))).ToDictionary(x => x.ProductId) : []; return items.Select(i => ToDto(i, products.TryGetValue(i.ProductId, out var p) ? p.ImageUrl : null)).ToList(); }
    private static ServiceResult<T> Fail<T>(string? error = null) => ServiceResult<T>.Failure(ServiceStatus.Invalid, error);
    private static int DealQuantity(Product product, ProductDeal? deal, int quantity) => deal is null ? 0 : Math.Min(quantity, Math.Min(product.Quantity, deal.TotalQuantity - deal.UsedQuantity));
    private static decimal LineTotal(Product product, ProductDeal? deal, int quantity) { var dealQuantity = DealQuantity(product, deal, quantity); return dealQuantity * PriceFor(product, deal) + (quantity - dealQuantity) * product.Price; }
    private static decimal PriceFor(Product product, ProductDeal? deal) => deal is null ? product.Price : Math.Max(0m, deal.DiscountType == "Percent" ? product.Price * (1m - deal.DiscountValue / 100m) : product.Price - deal.DiscountValue);
    private static OrderDto ToDto(Order o) => new() { OrderId = o.OrderId, ShopId = o.ShopId, OrderNumber = o.OrderNumber, Subtotal = o.Subtotal, ShippingFee = o.ShippingFee, TotalAmount = o.TotalAmount, ShippingMethod = o.ShippingMethod, OrderStatus = o.OrderStatus, PaymentStatus = o.PaymentStatus, PayoutStatus = o.PayoutStatus, CreatedAt = o.CreatedAt, ReceiverName = o.ReceiverName, ReceiverPhone = o.ReceiverPhone, ShippingAddress = o.ShippingAddress, SlipImageUrl = o.SlipImageUrl, SlipUploadedAt = o.SlipUploadedAt, PaymentExpiresAt = o.PaymentExpiresAt };
    private static OrderItemDto ToDto(OrderItem i, string? image) => new() { OrderItemId = i.OrderItemId, ProductId = i.ProductId, ProductName = i.ProductName, Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice, ImageUrl = image };
    private static ShipmentDto? ToDto(Shipment? s) => s is null ? null : new() { ShippingProvider = s.ShippingProvider, TrackingNumber = s.TrackingNumber, ShippingStatus = s.ShippingStatus, ShippedAt = s.ShippedAt, DeliveredAt = s.DeliveredAt };
    private static OrderDto WithShop(OrderDto dto, Shop shop) { dto.ShopName = shop.ShopName; dto.ShopLogoUrl = shop.CoverImageUrl; return dto; }
    private static void Copy(OrderDto source, OrderDto target) { target.OrderId = source.OrderId; target.ShopId = source.ShopId; target.OrderNumber = source.OrderNumber; target.Subtotal = source.Subtotal; target.ShippingFee = source.ShippingFee; target.TotalAmount = source.TotalAmount; target.ShippingMethod = source.ShippingMethod; target.OrderStatus = source.OrderStatus; target.PaymentStatus = source.PaymentStatus; target.PayoutStatus = source.PayoutStatus; target.CreatedAt = source.CreatedAt; target.ReceiverName = source.ReceiverName; target.ReceiverPhone = source.ReceiverPhone; target.ShippingAddress = source.ShippingAddress; target.SlipImageUrl = source.SlipImageUrl; target.SlipUploadedAt = source.SlipUploadedAt; target.PaymentExpiresAt = source.PaymentExpiresAt; }
    private static MerchantPayoutRecordDto ToPayoutDto(MerchantPayout p, Shop? s, IEnumerable<Order> orders) => new() { PayoutId = p.PayoutId, ShopId = p.ShopId, ShopName = s?.ShopName, ShopLogoUrl = s?.CoverImageUrl, TotalAmount = p.TotalAmount, SlipImageUrl = p.SlipImageUrl, TransactionRef = p.TransactionRef, Note = p.Note, Status = p.Status, CreatedAt = p.CreatedAt, OrdersCount = orders.Count(), Orders = orders.Select(ToDto).ToList() };
}
