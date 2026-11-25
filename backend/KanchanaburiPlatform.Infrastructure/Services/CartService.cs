using Application.DTOs;
using Core.Interfaces;
using KanchanaburiPlatform.Application.Specifications.Commerce;
using KanchanaburiPlatform.Domain.Entities;

namespace Infrastructure.Services;

public sealed class CartService(IUnitOfWork unit) : ICartService
{
    public async Task<CartDto> GetCartAsync(string userId) => await ToDtoAsync(await FindCartAsync(userId, includeItems: true));

    public async Task<CartOperationResult<CartDto>> AddItemAsync(string userId, AddCartItemDto dto)
    {
        var product = await unit.Repository<Product>().GetEntityWithSpec(new CartProductSearchSpecification(dto.ProductId, "Active"));
        if (product is null) return new(CartOperationStatus.ProductUnavailable, Message: "สินค้านี้ไม่พร้อมจำหน่าย");
        if (product.Quantity <= 0) return new(CartOperationStatus.OutOfStock, Message: "สินค้าหมดแล้ว");

        var cart = await FindCartAsync(userId);
        var isNewCart = cart is null;
        cart ??= AddCart(userId);
        var item = await unit.Repository<CartItem>().GetEntityWithSpec(new CartItemSearchSpecification(cartId: cart.CartId, productId: dto.ProductId));
        var requestedQuantity = (item?.Quantity ?? 0) + dto.Quantity;
        if (requestedQuantity > product.Quantity)
            return new(CartOperationStatus.OutOfStock, Message: $"สินค้านี้มีในสต็อก {product.Quantity} ชิ้น แต่ในตะกร้าของคุณมี {item?.Quantity ?? 0} ชิ้นแล้ว จึงไม่สามารถเพิ่มได้อีก");

        if (item is null)
            unit.Repository<CartItem>().Add(new CartItem { CartItemId = Guid.NewGuid(), CartId = cart.CartId, ProductId = dto.ProductId, Quantity = dto.Quantity, UnitPrice = product.Price });
        else { item.Quantity = requestedQuantity; item.UnitPrice = product.Price; unit.Repository<CartItem>().Update(item); }

        if (!isNewCart)
        {
            cart.UpdatedAt = DateTime.UtcNow;
            unit.Repository<Cart>().Update(cart);
        }
        if (!await unit.Complete()) return new(CartOperationStatus.SaveFailed);
        return new(CartOperationStatus.Success, await GetCartAsync(userId));
    }

    public async Task<CartOperationResult> UpdateItemAsync(string userId, Guid cartItemId, UpdateCartItemDto dto)
    {
        if (dto.Quantity <= 0) return new(CartOperationStatus.OutOfStock, "จำนวนสินค้าต้องมากกว่า 0");
        var item = await unit.Repository<CartItem>().GetEntityWithSpec(new CartItemSearchSpecification(cartItemId: cartItemId));
        if (item is null) return new(CartOperationStatus.NotFound);
        var cart = await unit.Repository<Cart>().GetEntityWithSpec(new CartSearchSpecification(cartId: item.CartId));
        if (cart?.UserId != userId) return new(CartOperationStatus.Forbidden);
        var product = await unit.Repository<Product>().GetEntityWithSpec(new CartProductSearchSpecification(item.ProductId, "Active"));
        if (product is null) return new(CartOperationStatus.ProductUnavailable, "สินค้านี้ไม่พร้อมจำหน่าย");
        if (dto.Quantity > product.Quantity) return new(CartOperationStatus.OutOfStock, $"สินค้านี้มีในสต็อก {product.Quantity} ชิ้น กรุณาเลือกจำนวนไม่เกิน {product.Quantity} ชิ้น");
        item.Quantity = dto.Quantity;
        unit.Repository<CartItem>().Update(item);
        cart.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Cart>().Update(cart);
        return await unit.Complete() ? new(CartOperationStatus.Success) : new(CartOperationStatus.SaveFailed);
    }

    public async Task<CartOperationResult> RemoveItemAsync(string userId, Guid cartItemId)
    {
        var item = await unit.Repository<CartItem>().GetEntityWithSpec(new CartItemSearchSpecification(cartItemId: cartItemId));
        if (item is null) return new(CartOperationStatus.NotFound);
        var cart = await unit.Repository<Cart>().GetEntityWithSpec(new CartSearchSpecification(cartId: item.CartId));
        if (cart?.UserId != userId) return new(CartOperationStatus.Forbidden);
        unit.Repository<CartItem>().Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Cart>().Update(cart);
        return await unit.Complete() ? new(CartOperationStatus.Success) : new(CartOperationStatus.SaveFailed);
    }

    private Task<Cart?> FindCartAsync(string userId, bool includeItems = false) => unit.Repository<Cart>().GetEntityWithSpec(new CartSearchSpecification(userId, includeItems: includeItems));
    private Cart AddCart(string userId) { var cart = new Cart { CartId = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; unit.Repository<Cart>().Add(cart); return cart; }
    private async Task<CartDto> ToDtoAsync(Cart? cart)
    {
        if (cart is null) return new();
        var now = DateTime.UtcNow;
        var ids = cart.Items.Select(item => item.ProductId).ToHashSet();
        var deals = (await unit.Repository<ProductDeal>().ListAsync(deal => deal.Status == "Active" && deal.StartsAt <= now && deal.EndsAt > now && deal.UsedQuantity < deal.TotalQuantity)).Where(deal => ids.Contains(deal.ProductId)).GroupBy(deal => deal.ProductId).ToDictionary(group => group.Key, group => group.OrderBy(deal => deal.EndsAt).First());
        var items = cart.Items.Select(item =>
        {
            deals.TryGetValue(item.ProductId, out var deal);
            var dealQuantity = deal is null ? 0 : Math.Min(item.Quantity, Math.Min(item.Product.Quantity, deal.TotalQuantity - deal.UsedQuantity));
            var normalQuantity = item.Quantity - dealQuantity;
            var dealPrice = deal is null ? item.Product.Price : Math.Max(0m, deal.DiscountType == "Percent" ? item.Product.Price * (1m - deal.DiscountValue / 100m) : item.Product.Price - deal.DiscountValue);
            return new CartItemDto { CartItemId = item.CartItemId, ProductId = item.ProductId, ShopId = item.Product.ShopId, ShopName = item.Product.Shop?.ShopName ?? "ร้านค้าชุมชน", ProductName = item.Product.ProductName, ImageUrl = item.Product.ImageUrl, Quantity = item.Quantity, AvailableQuantity = item.Product.Quantity, OriginalPrice = item.Product.Price, UnitPrice = dealQuantity > 0 ? dealPrice : item.Product.Price, DealQuantity = dealQuantity, NormalQuantity = normalQuantity, DealUnitPrice = dealPrice, NormalUnitPrice = item.Product.Price, DealType = deal?.DealType, DealDiscountValue = deal?.DiscountValue, DealEndsAt = deal?.EndsAt };
        }).ToList();
        return new CartDto { CartId = cart.CartId, Items = items, Total = items.Sum(item => item.Subtotal) };
    }
}
