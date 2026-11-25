using Application.DTOs;
using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace Infrastructure.Services;

public sealed class ProductDealService(IUnitOfWork unit) : IProductDealService
{
    public async Task<ServiceResult<IReadOnlyList<ProductDealDto>>> GetMineAsync(string userId)
    {
        var productIds = (await unit.Repository<Product>().ListAsync(product => product.Shop.OwnerUserId == userId)).Select(product => product.ProductId).ToHashSet();
        var deals = await unit.Repository<ProductDeal>().ListAsync(deal => productIds.Contains(deal.ProductId));
        return ServiceResult<IReadOnlyList<ProductDealDto>>.Success(deals.OrderByDescending(deal => deal.StartsAt).Select(ToDto).ToList());
    }
    public async Task<ServiceResult<IReadOnlyList<ProductDealDto>>> GetByProductAsync(Guid productId, string userId)
    {
        if (await OwnedProductAsync(productId, userId) is null) return ServiceResult<IReadOnlyList<ProductDealDto>>.Failure(ServiceStatus.Forbidden);
        var deals = await unit.Repository<ProductDeal>().ListAsync(deal => deal.ProductId == productId);
        return ServiceResult<IReadOnlyList<ProductDealDto>>.Success(deals.OrderByDescending(deal => deal.StartsAt).Select(ToDto).ToList());
    }

    public async Task<ServiceResult<ProductDealDto>> CreateAsync(Guid productId, SaveProductDealDto dto, string userId)
    {
        var product = await OwnedProductAsync(productId, userId);
        if (product is null) return ServiceResult<ProductDealDto>.Failure(ServiceStatus.Forbidden);
        var error = Validate(dto, product.Price); if (error is not null) return ServiceResult<ProductDealDto>.Failure(ServiceStatus.Invalid, error);
        var quantityError = await ValidateAvailableQuantityAsync(product, dto.TotalQuantity, 0, null);
        if (quantityError is not null) return ServiceResult<ProductDealDto>.Failure(ServiceStatus.Invalid, quantityError);
        var deal = new ProductDeal { ProductDealId = Guid.NewGuid(), ProductId = productId, DealType = dto.DealType, DiscountType = dto.DiscountType, DiscountValue = dto.DiscountValue, TotalQuantity = dto.TotalQuantity, StartsAt = dto.StartsAt, EndsAt = dto.EndsAt };
        unit.Repository<ProductDeal>().Add(deal);
        return await unit.Complete() ? ServiceResult<ProductDealDto>.Success(ToDto(deal)) : ServiceResult<ProductDealDto>.Failure(ServiceStatus.Failed);
    }

    public async Task<ServiceResult<ProductDealDto>> UpdateAsync(Guid dealId, SaveProductDealDto dto, string userId)
    {
        var deal = await unit.Repository<ProductDeal>().GetByIdAsync(dealId); if (deal is null) return ServiceResult<ProductDealDto>.Failure(ServiceStatus.NotFound);
        var product = await OwnedProductAsync(deal.ProductId, userId); if (product is null) return ServiceResult<ProductDealDto>.Failure(ServiceStatus.Forbidden);
        var error = Validate(dto, product.Price); if (error is not null) return ServiceResult<ProductDealDto>.Failure(ServiceStatus.Invalid, error);
        var quantityError = await ValidateAvailableQuantityAsync(product, dto.TotalQuantity, deal.UsedQuantity, deal.ProductDealId);
        if (quantityError is not null) return ServiceResult<ProductDealDto>.Failure(ServiceStatus.Invalid, quantityError);
        deal.DealType = dto.DealType; deal.DiscountType = dto.DiscountType; deal.DiscountValue = dto.DiscountValue; deal.TotalQuantity = dto.TotalQuantity; deal.StartsAt = dto.StartsAt; deal.EndsAt = dto.EndsAt; deal.UpdatedAt = DateTime.UtcNow;
        unit.Repository<ProductDeal>().Update(deal);
        return await unit.Complete() ? ServiceResult<ProductDealDto>.Success(ToDto(deal)) : ServiceResult<ProductDealDto>.Failure(ServiceStatus.Failed);
    }

    public async Task<ServiceResult<bool>> StopAsync(Guid dealId, string userId)
    {
        var deal = await unit.Repository<ProductDeal>().GetByIdAsync(dealId); if (deal is null) return ServiceResult<bool>.Failure(ServiceStatus.NotFound);
        if (await OwnedProductAsync(deal.ProductId, userId) is null) return ServiceResult<bool>.Failure(ServiceStatus.Forbidden);
        deal.Status = "Stopped"; deal.UpdatedAt = DateTime.UtcNow; unit.Repository<ProductDeal>().Update(deal);
        return await unit.Complete() ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(ServiceStatus.Failed);
    }

    private async Task<Product?> OwnedProductAsync(Guid productId, string userId) => (await unit.Repository<Product>().ListAsync(product => product.ProductId == productId && product.Shop.OwnerUserId == userId)).FirstOrDefault();
    private static string? Validate(SaveProductDealDto dto, decimal price) => dto.DealType is not ("FlashDeal" or "LocalDeal") ? "Invalid deal type" : dto.DiscountType is not ("FixedAmount" or "Percent") ? "Invalid discount type" : dto.DiscountValue <= 0 || (dto.DiscountType == "Percent" && dto.DiscountValue >= 100) || (dto.DiscountType == "FixedAmount" && dto.DiscountValue >= price) ? "Invalid discount value" : dto.TotalQuantity <= 0 || dto.EndsAt <= dto.StartsAt ? "Invalid deal schedule or quantity" : null;
    private async Task<string?> ValidateAvailableQuantityAsync(
        Product product,
        int totalQuantity,
        int usedQuantity,
        Guid? excludedDealId
    )
    {
        if (totalQuantity < usedQuantity)
        {
            return $"จำนวนสิทธิ์ต้องไม่น้อยกว่าที่ใช้ไปแล้ว {usedQuantity} สิทธิ์";
        }

        var otherReservedQuantity = (await unit.Repository<ProductDeal>()
            .ListAsync(deal =>
                deal.ProductId == product.ProductId &&
                deal.Status == "Active" &&
                deal.EndsAt > DateTime.UtcNow &&
                (!excludedDealId.HasValue || deal.ProductDealId != excludedDealId.Value)
            ))
            .Sum(deal => Math.Max(0, deal.TotalQuantity - deal.UsedQuantity));

        var requestedRemainingQuantity = totalQuantity - usedQuantity;

        if (requestedRemainingQuantity + otherReservedQuantity > product.Quantity)
        {
            return $"สิทธิ์โปรโมชันที่เหลือรวมกันต้องไม่เกินสต็อกสินค้า {product.Quantity} ชิ้น";
        }

        return null;
    }

    private static ProductDealDto ToDto(ProductDeal deal) => new() { ProductDealId = deal.ProductDealId, ProductId = deal.ProductId, DealType = deal.DealType, DiscountType = deal.DiscountType, DiscountValue = deal.DiscountValue, TotalQuantity = deal.TotalQuantity, UsedQuantity = deal.UsedQuantity, AvailableQuantity = Math.Max(0, deal.TotalQuantity - deal.UsedQuantity), StartsAt = deal.StartsAt, EndsAt = deal.EndsAt, Status = deal.Status };
}
