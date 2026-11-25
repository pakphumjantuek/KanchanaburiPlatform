using Application.DTOs;
using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Application.Specifications.Commerce;
using KanchanaburiPlatform.Domain.Entities;

namespace Infrastructure.Services;

public sealed class ProductCategoryService(
    IUnitOfWork unit
) : IProductCategoryService
{
    public async Task<PagedResultDto<ProductCategoryDto>> GetCategoriesAsync(
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var criteria = new ProductCategorySearchSpecification();
        var pageSpecification = new ProductCategorySearchSpecification(
            page: page,
            pageSize: pageSize);
        var totalCount = await unit.Repository<ProductCategory>()
            .CountAsync(criteria);
        var categories = await unit.Repository<ProductCategory>()
            .ListAsync(pageSpecification);

        return new PagedResultDto<ProductCategoryDto>
        {
            Items = categories.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ServiceResult<ProductCategoryDto>> GetCategoryAsync(Guid id)
    {
        var category = await FindAsync(id);

        if (category is null)
        {
            return NotFound<ProductCategoryDto>();
        }

        return ServiceResult<ProductCategoryDto>.Success(ToDto(category));
    }

    public async Task<ServiceResult<ProductCategoryDto>> CreateAsync(
        CreateProductCategoryDto dto)
    {
        var category = new ProductCategory
        {
            ProductCategoryId = Guid.NewGuid(),
            CategoryName = dto.CategoryName,
            Description = dto.Description,
            Status = "Active"
        };

        unit.Repository<ProductCategory>().Add(category);

        if (!await unit.Complete())
        {
            return Invalid<ProductCategoryDto>();
        }

        return ServiceResult<ProductCategoryDto>.Success(ToDto(category));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(
        Guid id,
        UpdateProductCategoryDto dto)
    {
        var category = await FindAsync(id);

        if (category is null)
        {
            return NotFound<bool>();
        }

        category.CategoryName = dto.CategoryName;
        category.Description = dto.Description;
        category.Status = dto.Status;
        unit.Repository<ProductCategory>().Update(category);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Invalid<bool>();
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var category = await FindAsync(id);

        if (category is null)
        {
            return NotFound<bool>();
        }

        category.Status = "Inactive";
        unit.Repository<ProductCategory>().Update(category);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Invalid<bool>();
    }

    private async Task<ProductCategory?> FindAsync(Guid id)
    {
        var specification = new ProductCategorySearchSpecification(
            categoryId: id);

        return await unit.Repository<ProductCategory>()
            .GetEntityWithSpec(specification);
    }

    private static ProductCategoryDto ToDto(ProductCategory category)
    {
        return new ProductCategoryDto
        {
            ProductCategoryId = category.ProductCategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Status = category.Status
        };
    }

    private static ServiceResult<T> NotFound<T>()
    {
        return ServiceResult<T>.Failure(ServiceStatus.NotFound);
    }

    private static ServiceResult<T> Invalid<T>()
    {
        return ServiceResult<T>.Failure(ServiceStatus.Invalid);
    }
}
