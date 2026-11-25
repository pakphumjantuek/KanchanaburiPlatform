namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ProductDeal
{
    public Guid ProductDealId { get; set; }
    public Guid ProductId { get; set; }
    public string DealType { get; set; } = "FlashDeal";
    public string DiscountType { get; set; } = "FixedAmount";
    public decimal DiscountValue { get; set; }
    public int TotalQuantity { get; set; }
    public int UsedQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
}
