namespace Application.DTOs;

public sealed class CreateProductReviewDto
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class ProductReviewDto
{
    public Guid ReviewId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? Reply { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ProductReviewsDto
{
    public int TotalCount { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<int, int> RatingCounts { get; set; } = [];
    public List<ProductReviewDto> Reviews { get; set; } = [];
}
