using Application.DTOs;
using Application.Results;

namespace Core.Interfaces;

public interface IProductReviewService
{
    Task<ServiceResult<ProductReviewsDto>> GetReviewsAsync(Guid productId);
    Task<IReadOnlyList<Guid>> GetReviewedProductIdsAsync(string userId, Guid orderId);
    Task<ServiceResult<ProductReviewDto>> CreateAsync(
        CreateProductReviewDto dto,
        string userId);
}
