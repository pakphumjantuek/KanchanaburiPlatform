using Application.DTOs;
using Application.Results;

namespace Core.Interfaces;

public interface IProductCategoryService
{
    Task<PagedResultDto<ProductCategoryDto>> GetCategoriesAsync(
        int page,
        int pageSize);

    Task<ServiceResult<ProductCategoryDto>> GetCategoryAsync(Guid id);
    Task<ServiceResult<ProductCategoryDto>> CreateAsync(CreateProductCategoryDto dto);
    Task<ServiceResult<bool>> UpdateAsync(Guid id, UpdateProductCategoryDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
