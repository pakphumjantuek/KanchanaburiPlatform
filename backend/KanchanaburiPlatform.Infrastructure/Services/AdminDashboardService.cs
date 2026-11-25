using Application.DTOs;
using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Application.Common.Export;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AdminDashboardService(
    StoreContext context
) : IAdminDashboardService
{
    public async Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync(
        DateTime? from,
        DateTime? to)
    {
        if (!DateRangeHelper.TryRange(from, to, out var rangeFrom, out var rangeTo, out var error))
        {
            return ServiceResult<AdminDashboardDto>.Failure(
                ServiceStatus.Invalid,
                error);
        }

        var ordersInRange = context.Orders.AsNoTracking()
            .Where(order => order.CreatedAt >= rangeFrom && order.CreatedAt < rangeTo);

        var paidOrdersInRange = ordersInRange
            .Where(order => order.PaymentStatus == "Paid");

        var totalOrders = await ordersInRange.CountAsync();
        var paidOrders = await paidOrdersInRange.CountAsync();

        var totalSales = await paidOrdersInRange
            .Select(order => (decimal?)order.TotalAmount)
            .SumAsync() ?? 0m;

        var statusCounts = await ordersInRange
            .GroupBy(order => new { order.OrderStatus, order.PaymentStatus })
            .Select(group => new
            {
                group.Key.OrderStatus,
                group.Key.PaymentStatus,
                Count = group.Count()
            })
            .ToListAsync();

        var salesTrend = await paidOrdersInRange
            .GroupBy(order => order.CreatedAt.Date)
            .Select(group => new AdminSalesTrendDto
            {
                Date = group.Key,
                Sales = group.Sum(order => order.TotalAmount),
                Orders = group.Count()
            })
            .OrderBy(item => item.Date)
            .ToListAsync();

        var recentOrders = await context.Orders.AsNoTracking()
            .OrderByDescending(order => order.CreatedAt)
            .Take(10)
            .Select(order => new AdminRecentOrderDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                ShopName = order.Shop.ShopName,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt
            })
            .ToListAsync();

        var topShops = await paidOrdersInRange
            .GroupBy(order => new { order.ShopId, order.Shop.ShopName })
            .Select(group => new AdminTopShopDto
            {
                ShopId = group.Key.ShopId,
                ShopName = group.Key.ShopName,
                Sales = group.Sum(order => order.TotalAmount),
                PaidOrders = group.Count()
            })
            .OrderByDescending(shop => shop.Sales)
            .Take(5)
            .ToListAsync();

        var pendingPayoutAmount = await context.Orders.AsNoTracking()
            .Where(order => order.PaymentStatus == "Paid" && order.PayoutStatus != "Settled")
            .Select(order => (decimal?)order.TotalAmount)
            .SumAsync() ?? 0m;
        var pendingSlips = await context.Orders.AsNoTracking()
            .CountAsync(order => order.PaymentStatus == "PendingVerification");
        var pendingPayoutOrders = await context.Orders.AsNoTracking()
            .CountAsync(order => order.PaymentStatus == "Paid" && order.PayoutStatus != "Settled");
        var pendingShipmentOrders = await context.Orders.AsNoTracking()
            .CountAsync(order => order.PaymentStatus == "Paid" &&
                (order.OrderStatus == "Pending" || order.OrderStatus == "Confirmed" || order.OrderStatus == "Processing"));

        var countByOrderStatus = statusCounts
            .GroupBy(item => item.OrderStatus)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Count));
        var pendingPayment = statusCounts
            .Where(item => item.PaymentStatus == "Pending")
            .Sum(item => item.Count);
        var pendingVerification = statusCounts
            .Where(item => item.PaymentStatus == "PendingVerification")
            .Sum(item => item.Count);

        var dashboard = new AdminDashboardDto
        {
            From = rangeFrom,
            To = rangeTo.AddTicks(-1),
            Summary = new AdminDashboardSummaryDto
            {
                TotalSales = totalSales,
                TotalOrders = totalOrders,
                PaidOrders = paidOrders,
                PendingPayoutAmount = pendingPayoutAmount,
                TotalUsers = await context.Users.AsNoTracking().CountAsync(),
                ActiveShops = await context.Shops.AsNoTracking().CountAsync(shop => shop.Status == "Active"),
                LowStockProducts = await context.Products.AsNoTracking().CountAsync(product => product.Status == "Active" && product.Quantity <= 5)
            },
            OrderStatusCounts = new AdminOrderStatusCountsDto
            {
                PendingPayment = pendingPayment,
                PendingVerification = pendingVerification,
                Processing = CountOrderStatus(countByOrderStatus, "Processing") + CountOrderStatus(countByOrderStatus, "Confirmed"),
                Shipped = CountOrderStatus(countByOrderStatus, "Shipped"),
                Completed = CountOrderStatus(countByOrderStatus, "Completed"),
                Cancelled = CountOrderStatus(countByOrderStatus, "Cancelled")
            },
            SalesTrend = salesTrend,
            RecentOrders = recentOrders,
            TopShops = topShops,
            Alerts = new AdminDashboardAlertsDto
            {
                PendingSlips = pendingSlips,
                PendingPayoutOrders = pendingPayoutOrders,
                PendingShipmentOrders = pendingShipmentOrders
            }
        };

        return ServiceResult<AdminDashboardDto>.Success(dashboard);
    }

    private static int CountOrderStatus(
        IReadOnlyDictionary<string, int> counts,
        string status)
    {
        return counts.GetValueOrDefault(status);
    }
}
