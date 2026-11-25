namespace Application.DTOs;

public sealed class ProductDealDto
{
    public Guid ProductDealId { get; set; }
    public Guid ProductId { get; set; }
    public string DealType { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public int TotalQuantity { get; set; }
    public int UsedQuantity { get; set; }
    public int RemainingQuantity => Math.Max(0, TotalQuantity - UsedQuantity);
    public int AvailableQuantity { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class SaveProductDealDto
{
    public string DealType { get; set; } = "FlashDeal";
    public string DiscountType { get; set; } = "FixedAmount";
    public decimal DiscountValue { get; set; }
    public int TotalQuantity { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
}
