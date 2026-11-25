using Application.DTOs;
using Application.Results;

namespace Core.Interfaces;

public interface IProductDealService
{
    Task<ServiceResult<IReadOnlyList<ProductDealDto>>> GetMineAsync(string userId);
    Task<ServiceResult<IReadOnlyList<ProductDealDto>>> GetByProductAsync(Guid productId, string userId);
    Task<ServiceResult<ProductDealDto>> CreateAsync(Guid productId, SaveProductDealDto dto, string userId);
    Task<ServiceResult<ProductDealDto>> UpdateAsync(Guid dealId, SaveProductDealDto dto, string userId);
    Task<ServiceResult<bool>> StopAsync(Guid dealId, string userId);
}
