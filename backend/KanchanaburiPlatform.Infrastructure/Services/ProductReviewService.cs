using Application.DTOs;
using Application.Results;
using Core.Entities;
using Core.Interfaces;
using KanchanaburiPlatform.Application.Specifications.Commerce;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

public sealed class ProductReviewService(
    IUnitOfWork unit,
    UserManager<AppUser> userManager
) : IProductReviewService
{
    public async Task<ServiceResult<ProductReviewsDto>> GetReviewsAsync(
        Guid productId)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(productId);

        if (product?.Status != "Active")
        {
            return NotFound<ProductReviewsDto>();
        }

        var reviews = await unit.Repository<Review>().ListAsync(
            new ProductReviewSearchSpecification(
                productId: productId,
                status: "Published"));
        var ratingCounts = new Dictionary<int, int>
        {
            [5] = 0,
            [4] = 0,
            [3] = 0,
            [2] = 0,
            [1] = 0
        };

        foreach (var review in reviews)
        {
            if (ratingCounts.ContainsKey(review.Rating))
            {
                ratingCounts[review.Rating]++;
            }
        }

        var items = await Task.WhenAll(reviews.Select(ToDtoAsync));

        return ServiceResult<ProductReviewsDto>.Success(
            new ProductReviewsDto
            {
                TotalCount = reviews.Count,
                AverageRating = reviews.Count == 0
                    ? 0
                    : Math.Round(reviews.Average(review => review.Rating), 1),
                RatingCounts = ratingCounts,
                Reviews = items.ToList()
            });
    }

    public async Task<IReadOnlyList<Guid>> GetReviewedProductIdsAsync(
        string userId,
        Guid orderId)
    {
        var reviews = await unit.Repository<Review>().ListAsync(
            review => review.UserId == userId && review.OrderId == orderId);

        return reviews
            .Where(review => review.ProductId.HasValue)
            .Select(review => review.ProductId!.Value)
            .Distinct()
            .ToList();
    }

    public async Task<ServiceResult<ProductReviewDto>> CreateAsync(
        CreateProductReviewDto dto,
        string userId)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return Invalid<ProductReviewDto>("คะแนนต้องอยู่ระหว่าง 1 ถึง 5 ดาว");
        }

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return Invalid<ProductReviewDto>("กรุณากรอกข้อความรีวิว");
        }

        var product = await unit.Repository<Product>().GetByIdAsync(dto.ProductId);

        if (product is null)
        {
            return NotFound<ProductReviewDto>("ไม่พบสินค้า");
        }

        var order = await unit.Repository<Order>().GetByIdAsync(dto.OrderId);
        var hasPurchasedProduct = order?.UserId == userId &&
            order.OrderStatus == "Completed" &&
            (await unit.Repository<OrderItem>().ListAsync(item =>
                item.OrderId == dto.OrderId && item.ProductId == dto.ProductId)).Any();

        if (!hasPurchasedProduct)
        {
            return Invalid<ProductReviewDto>("คุณสามารถเขียนรีวิวได้เฉพาะสินค้าจากออเดอร์ที่สำเร็จสมบูรณ์แล้วเท่านั้น");
        }

        var existingReview = await unit.Repository<Review>().FirstOrDefaultAsync(
            review => review.ProductId == dto.ProductId &&
                review.UserId == userId &&
                review.OrderId == dto.OrderId);

        if (existingReview is not null)
        {
            existingReview.Rating = dto.Rating;
            existingReview.Comment = dto.Comment.Trim();
            existingReview.CreatedAt = DateTime.UtcNow;
            existingReview.Status = "Published";
            unit.Repository<Review>().Update(existingReview);

            return await unit.Complete()
                ? ServiceResult<ProductReviewDto>.Success(
                    await ToDtoAsync(existingReview))
                : Invalid<ProductReviewDto>("ไม่สามารถบันทึกรีวิวได้");
        }

        var review = new Review
        {
            ReviewId = Guid.NewGuid(),
            UserId = userId,
            ProductId = dto.ProductId,
            ShopId = product.ShopId,
            OrderId = dto.OrderId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            Status = "Published",
            CreatedAt = DateTime.UtcNow
        };

        unit.Repository<Review>().Add(review);

        return await unit.Complete()
            ? ServiceResult<ProductReviewDto>.Success(await ToDtoAsync(review))
            : Invalid<ProductReviewDto>("ไม่สามารถบันทึกรีวิวได้");
    }

    private async Task<ProductReviewDto> ToDtoAsync(Review review)
    {
        var user = await userManager.FindByIdAsync(review.UserId);
        var userName = $"{user?.FirstName} {user?.LastName}".Trim();

        return new ProductReviewDto
        {
            ReviewId = review.ReviewId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "ผู้ใช้งาน" : userName,
            Rating = review.Rating,
            Comment = review.Comment,
            Reply = review.Reply,
            CreatedAt = review.CreatedAt
        };
    }

    private static ServiceResult<T> NotFound<T>(string? error = null)
    {
        return ServiceResult<T>.Failure(ServiceStatus.NotFound, error);
    }

    private static ServiceResult<T> Invalid<T>(string error)
    {
        return ServiceResult<T>.Failure(ServiceStatus.Invalid, error);
    }
}
