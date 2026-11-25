using Application.DTOs;
using Application.Results;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync();
    Task<IReadOnlyList<ProductDto>> GetActiveDealProductsAsync(int limit);
    Task<ServiceResult<ProductDto>> GetProductAsync(Guid id);
    Task<PagedResultDto<ProductDto>> GetProductsForAdminAsync(Guid? shopId, string? status, int page, int pageSize);
    Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, UpdateProductStatusDto dto);
    Task<ServiceResult<ProductDto>> CreateAsync(CreateProductDto dto, string? userId, bool isAdmin);
    Task<ServiceResult<bool>> UpdateAsync(Guid id, UpdateProductDto dto, string? userId, bool isAdmin);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, string? userId, bool isAdmin);
    Task<ServiceResult<ProductDto>> UploadCoverImageAsync(Guid id, IFormFile file, string? userId, bool isAdmin, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<string>>> UploadDetailImagesAsync(Guid id, List<IFormFile> files, string? userId, bool isAdmin, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteDetailImageAsync(Guid id, int imageIndex, string? userId, bool isAdmin);
}
