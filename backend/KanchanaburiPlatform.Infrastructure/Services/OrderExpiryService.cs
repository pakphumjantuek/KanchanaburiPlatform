using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace Infrastructure.Services;

public sealed class OrderExpiryService(IUnitOfWork unit) : IOrderExpiryService
{
    public async Task<int> ExpirePendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var orders = await unit.Repository<Order>().ListAsync(order =>
            order.OrderStatus == "Pending" && order.PaymentStatus == "Pending" &&
            order.PaymentExpiresAt.HasValue && order.PaymentExpiresAt <= now);

        foreach (var order in orders)
        {
            await ExpireAsync(order, cancellationToken);
        }

        return orders.Count == 0 || await unit.Complete() ? orders.Count : 0;
    }

    public async Task<bool> ExpireIfNeededAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(orderId);
        if (order is null || order.OrderStatus != "Pending" || order.PaymentStatus != "Pending" ||
            !order.PaymentExpiresAt.HasValue || order.PaymentExpiresAt > DateTime.UtcNow)
        {
            return false;
        }

        await ExpireAsync(order, cancellationToken);
        await unit.Complete();
        return true;
    }

    private async Task ExpireAsync(Order order, CancellationToken cancellationToken)
    {
        var items = await unit.Repository<OrderItem>().ListAsync(item => item.OrderId == order.OrderId);
        foreach (var item in items)
        {
            var product = await unit.Repository<Product>().GetByIdAsync(item.ProductId);
            if (product is null) continue;
            product.Quantity += item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
            unit.Repository<Product>().Update(product);
            if (item.ProductDealId.HasValue)
            {
                var deal = await unit.Repository<ProductDeal>().GetByIdAsync(item.ProductDealId.Value);
                if (deal is not null)
                {
                    deal.UsedQuantity = Math.Max(0, deal.UsedQuantity - item.Quantity);
                    deal.UpdatedAt = DateTime.UtcNow;
                    unit.Repository<ProductDeal>().Update(deal);
                }
            }
        }

        order.OrderStatus = "Expired";
        order.PaymentStatus = "Expired";
        unit.Repository<Order>().Update(order);
    }
}
