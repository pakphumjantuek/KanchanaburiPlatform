
using System.Text.Json;
using Application.DTOs;
using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Application.Specifications.Commerce;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public sealed class ProductService(
    IUnitOfWork unit,
    IImageUploadService imageUploadService
) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync()
    {
        var spec = new ProductSearchSpecification(status: "Active");
        var products = await unit.Repository<Product>().ListAsync(spec);

        var now = DateTime.UtcNow;
        var deals = await unit.Repository<ProductDeal>().ListAsync(deal => deal.Status == "Active" && deal.StartsAt <= now && deal.EndsAt > now && deal.UsedQuantity < deal.TotalQuantity);
        return products.Select(product => ToDto(product, activeDeal: deals.Where(deal => deal.ProductId == product.ProductId).OrderBy(deal => deal.EndsAt).FirstOrDefault())).ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetActiveDealProductsAsync(
        int limit
    )
    {
        limit = Math.Clamp(limit, 1, 24);

        var now = DateTime.UtcNow;
        var activeDeals = await unit.Repository<ProductDeal>().ListAsync(deal =>
            deal.Status == "Active" &&
            deal.StartsAt <= now &&
            deal.EndsAt > now &&
            deal.UsedQuantity < deal.TotalQuantity
        );

        var productIds = activeDeals
            .Select(deal => deal.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return [];
        }

        var products = await unit.Repository<Product>().ListAsync(product =>
            productIds.Contains(product.ProductId) &&
            product.Status == "Active" &&
            product.Quantity > 0
        );

        var activeShops = (await unit.Repository<Shop>().ListAsync(shop =>
            shop.Status == "Active"
        )).ToDictionary(shop => shop.ShopId);

        return products
            .Where(product => activeShops.ContainsKey(product.ShopId))
            .Select(product =>
            {
                var deal = activeDeals
                    .Where(item => item.ProductId == product.ProductId)
                    .OrderBy(item => item.EndsAt)
                    .First();

                return ToDto(
                    product,
                    activeShops[product.ShopId].ShopName,
                    deal
                );
            })
            .OrderBy(product => product.ActiveDeal!.EndsAt)
            .Take(limit)
            .ToList();
    }

    public async Task<ServiceResult<ProductDto>> GetProductAsync(Guid id)
    {
        var product = await FindAsync(id, "Active");

        if (product is null)
            return NotFound<ProductDto>();

        var now = DateTime.UtcNow;
        var deal = (await unit.Repository<ProductDeal>().ListAsync(item => item.ProductId == id && item.Status == "Active" && item.StartsAt <= now && item.EndsAt > now && item.UsedQuantity < item.TotalQuantity)).OrderBy(item => item.EndsAt).FirstOrDefault();
        return ServiceResult<ProductDto>.Success(ToDto(product, activeDeal: deal));
    }

    public async Task<PagedResultDto<ProductDto>> GetProductsForAdminAsync(
        Guid? shopId,
        string? status,
        int page,
        int pageSize
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var criteria = new ProductSearchSpecification(
            shopId: shopId,
            status: status
        );

        var spec = new ProductSearchSpecification(
            shopId: shopId,
            status: status,
            page: page,
            pageSize: pageSize
        );

        var products = await unit.Repository<Product>().ListAsync(spec);

        var shops = (await unit.Repository<Shop>().ListAllAsync())
            .ToDictionary(x => x.ShopId);

        var count = await unit.Repository<Product>().CountAsync(criteria);

        return new PagedResultDto<ProductDto>
        {
            Items = products.Select(product =>
            {
                shops.TryGetValue(product.ShopId, out var shop);

                return ToDto(product, shop?.ShopName);
            }).ToList(),

            Page = page,
            PageSize = pageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };
    }

    public async Task<ServiceResult<bool>> UpdateStatusAsync(
        Guid id,
        UpdateProductStatusDto dto
    )
    {
        var product = await FindAsync(id);

        if (product is null)
            return NotFound<bool>();

        if (dto.Status is not ("Active" or "Inactive"))
            return Invalid<bool>("Invalid product status");

        product.Status = dto.Status;
        product.UpdatedAt = DateTime.UtcNow;

        unit.Repository<Product>().Update(product);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Invalid<bool>("Problem updating product status");
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(
        CreateProductDto dto,
        string? userId,
        bool isAdmin
    )
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(dto.ShopId);

        if (shop is null)
            return Invalid<ProductDto>("Shop not found");

        if (!CanManage(shop, userId, isAdmin))
            return Forbidden<ProductDto>();

        var category = await unit
            .Repository<ProductCategory>()
            .GetByIdAsync(dto.ProductCategoryId);

        if (category is null)
            return Invalid<ProductDto>("Product category not found");

        var now = DateTime.UtcNow;

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            ShopId = dto.ShopId,
            ProductCategoryId = dto.ProductCategoryId,
            ProductName = dto.ProductName,
            Description = dto.Description,
            Price = dto.Price,
            Quantity = dto.Quantity,
            ImageUrl = dto.ImageUrl,
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now
        };

        unit.Repository<Product>().Add(product);

        return await unit.Complete()
            ? ServiceResult<ProductDto>.Success(ToDto(product))
            : Invalid<ProductDto>("Problem creating product");
    }

    public async Task<ServiceResult<bool>> UpdateAsync(
        Guid id,
        UpdateProductDto dto,
        string? userId,
        bool isAdmin
    )
    {
        var product = await FindAsync(id);

        if (product is null)
            return NotFound<bool>();

        var shop = await unit.Repository<Shop>().GetByIdAsync(product.ShopId);

        if (!CanManage(shop, userId, isAdmin))
            return Forbidden<bool>();

        var category = await unit
            .Repository<ProductCategory>()
            .GetByIdAsync(dto.ProductCategoryId);

        if (category is null)
            return Invalid<bool>("Product category not found");

        var remainingDealQuantity = (await unit.Repository<ProductDeal>()
            .ListAsync(deal =>
                deal.ProductId == product.ProductId &&
                deal.Status == "Active" &&
                deal.EndsAt > DateTime.UtcNow
            ))
            .Sum(deal => Math.Max(0, deal.TotalQuantity - deal.UsedQuantity));

        if (dto.Quantity < remainingDealQuantity)
        {
            return Invalid<bool>(
                $"สต็อกต้องไม่น้อยกว่าสิทธิ์โปรโมชันที่เหลือ {remainingDealQuantity} ชิ้น"
            );
        }

        product.ProductCategoryId = dto.ProductCategoryId;
        product.ProductName = dto.ProductName;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Quantity = dto.Quantity;
        product.ImageUrl = dto.ImageUrl;
        product.Status = dto.Status;
        product.UpdatedAt = DateTime.UtcNow;

        unit.Repository<Product>().Update(product);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Invalid<bool>("Problem updating product");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid id,
        string? userId,
        bool isAdmin
    )
    {
        var product = await FindAsync(id);

        if (product is null)
            return NotFound<bool>();

        var shop = await unit.Repository<Shop>().GetByIdAsync(product.ShopId);

        if (!CanManage(shop, userId, isAdmin))
            return Forbidden<bool>();

        product.Status = "Inactive";
        product.UpdatedAt = DateTime.UtcNow;

        unit.Repository<Product>().Update(product);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Invalid<bool>("Problem deleting product");
    }

    public async Task<ServiceResult<ProductDto>> UploadCoverImageAsync(
        Guid id,
        IFormFile file,
        string? userId,
        bool isAdmin,
        CancellationToken ct = default
    )
    {
        var result = await ManagedAsync(id, userId, isAdmin);

        if (result.Status != ServiceStatus.Success)
            return new(result.Status, Error: result.Error);

        var image = await imageUploadService.SaveAsync(file, "products", ct);

        if (image is null)
            return Invalid<ProductDto>(
                "Only JPG, PNG, WEBP files up to 5 MB are allowed."
            );

        var product = result.Data!;

        product.ImageUrl = image.Url;
        product.UpdatedAt = DateTime.UtcNow;

        unit.Repository<Product>().Update(product);

        return await unit.Complete()
            ? ServiceResult<ProductDto>.Success(ToDto(product))
            : Invalid<ProductDto>("Problem uploading cover image");
    }

    public async Task<ServiceResult<IReadOnlyList<string>>> UploadDetailImagesAsync(
        Guid id,
        List<IFormFile> files,
        string? userId,
        bool isAdmin,
        CancellationToken ct = default
    )
    {
        var result = await ManagedAsync(id, userId, isAdmin);

        if (result.Status != ServiceStatus.Success)
            return new(result.Status, Error: result.Error);

        if (files.Count is 0 or > 8)
            return Invalid<IReadOnlyList<string>>(
                "Upload 1 to 8 images at a time."
            );

        var product = result.Data!;
        var images = DetailImages(product);

        foreach (var file in files)
        {
            var image = await imageUploadService.SaveAsync(
                file,
                "products",
                ct
            );

            if (image is null)
                return Invalid<IReadOnlyList<string>>(
                    "Only JPG, PNG, WEBP files up to 5 MB are allowed."
                );

            images.Add(image.Url);
        }

        product.DetailImageUrls = JsonSerializer.Serialize(images);
        product.UpdatedAt = DateTime.UtcNow;

        unit.Repository<Product>().Update(product);

        return await unit.Complete()
            ? ServiceResult<IReadOnlyList<string>>.Success(images)
            : Invalid<IReadOnlyList<string>>(
                "Problem uploading detail images"
            );
    }

    public async Task<ServiceResult<bool>> DeleteDetailImageAsync(
        Guid id,
        int imageIndex,
        string? userId,
        bool isAdmin
    )
    {
        var result = await ManagedAsync(id, userId, isAdmin);

        if (result.Status != ServiceStatus.Success)
            return new(result.Status, Error: result.Error);

        var product = result.Data!;
        var images = DetailImages(product);

        if (imageIndex < 0 || imageIndex >= images.Count)
            return NotFound<bool>();

        images.RemoveAt(imageIndex);

        product.DetailImageUrls = JsonSerializer.Serialize(images);
        product.UpdatedAt = DateTime.UtcNow;

        unit.Repository<Product>().Update(product);

        return await unit.Complete()
            ? ServiceResult<bool>.Success(true)
            : Invalid<bool>("Problem deleting detail image");
    }

    private async Task<Product?> FindAsync(
        Guid id,
        string? status = null
    )
    {
        var spec = new ProductSearchSpecification(
            productId: id,
            status: status
        );

        return await unit
            .Repository<Product>()
            .GetEntityWithSpec(spec);
    }

    private async Task<ServiceResult<Product>> ManagedAsync(
        Guid id,
        string? userId,
        bool isAdmin
    )
    {
        var product = await FindAsync(id);

        if (product is null)
            return NotFound<Product>();

        var shop = await unit.Repository<Shop>().GetByIdAsync(product.ShopId);

        if (!CanManage(shop, userId, isAdmin))
            return Forbidden<Product>();

        return ServiceResult<Product>.Success(product);
    }

    private static bool CanManage(
        Shop? shop,
        string? userId,
        bool isAdmin
    )
    {
        return shop is not null &&
               (
                   isAdmin ||
                   (
                       shop.Status == "Active" &&
                       shop.OwnerUserId == userId
                   )
               );
    }

    private static List<string> DetailImages(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.DetailImageUrls))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(
                product.DetailImageUrls
            ) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static ProductDto ToDto(
        Product product,
        string? shopName = null,
        ProductDeal? activeDeal = null
    )
    {
        return new ProductDto
        {
            ProductId = product.ProductId,
            ShopId = product.ShopId,
            ShopName = shopName,
            ProductCategoryId = product.ProductCategoryId,
            ProductName = product.ProductName,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            RowVersion = product.RowVersion,
            ImageUrl = product.ImageUrl,
            Status = product.Status,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            DetailImages = DetailImages(product),
            ActiveDeal = activeDeal is null ? null : new ProductDealDto
            {
                ProductDealId = activeDeal.ProductDealId,
                ProductId = activeDeal.ProductId,
                DealType = activeDeal.DealType,
                DiscountType = activeDeal.DiscountType,
                DiscountValue = activeDeal.DiscountValue,
                TotalQuantity = activeDeal.TotalQuantity,
                UsedQuantity = activeDeal.UsedQuantity,
                AvailableQuantity = Math.Min(
                    product.Quantity,
                    Math.Max(0, activeDeal.TotalQuantity - activeDeal.UsedQuantity)
                ),
                StartsAt = activeDeal.StartsAt,
                EndsAt = activeDeal.EndsAt,
                Status = activeDeal.Status
            }
        };
    }

    private static ServiceResult<T> NotFound<T>()
    {
        return ServiceResult<T>.Failure(ServiceStatus.NotFound);
    }

    private static ServiceResult<T> Forbidden<T>()
    {
        return ServiceResult<T>.Failure(ServiceStatus.Forbidden);
    }

    private static ServiceResult<T> Invalid<T>(string? error = null)
    {
        return ServiceResult<T>.Failure(
            ServiceStatus.Invalid,
            error
        );
    }
}
