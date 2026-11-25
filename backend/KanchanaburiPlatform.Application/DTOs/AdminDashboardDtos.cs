namespace Application.DTOs;

public sealed class AdminDashboardDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public AdminDashboardSummaryDto Summary { get; set; } = new();
    public AdminOrderStatusCountsDto OrderStatusCounts { get; set; } = new();
    public List<AdminSalesTrendDto> SalesTrend { get; set; } = [];
    public List<AdminRecentOrderDto> RecentOrders { get; set; } = [];
    public List<AdminTopShopDto> TopShops { get; set; } = [];
    public AdminDashboardAlertsDto Alerts { get; set; } = new();
}

public sealed class AdminDashboardSummaryDto
{
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public decimal PendingPayoutAmount { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveShops { get; set; }
    public int LowStockProducts { get; set; }
}

public sealed class AdminOrderStatusCountsDto
{
    public int PendingPayment { get; set; }
    public int PendingVerification { get; set; }
    public int Processing { get; set; }
    public int Shipped { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}

public sealed class AdminSalesTrendDto
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public int Orders { get; set; }
}

public sealed class AdminRecentOrderDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ShopName { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class AdminTopShopDto
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public int PaidOrders { get; set; }
}

public sealed class AdminDashboardAlertsDto
{
    public int PendingSlips { get; set; }
    public int PendingPayoutOrders { get; set; }
    public int PendingShipmentOrders { get; set; }
}
