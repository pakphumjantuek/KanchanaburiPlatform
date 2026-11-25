using Application.DTOs;

namespace Core.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<CartOperationResult<CartDto>> AddItemAsync(string userId, AddCartItemDto dto);
    Task<CartOperationResult> UpdateItemAsync(string userId, Guid cartItemId, UpdateCartItemDto dto);
    Task<CartOperationResult> RemoveItemAsync(string userId, Guid cartItemId);
}

public enum CartOperationStatus { Success, NotFound, Forbidden, ProductUnavailable, OutOfStock, SaveFailed }

public sealed record CartOperationResult(CartOperationStatus Status, string? Message = null)
{
    public bool Succeeded => Status == CartOperationStatus.Success;
}

public sealed record CartOperationResult<T>(CartOperationStatus Status, T? Data = default, string? Message = null)
{
    public bool Succeeded => Status == CartOperationStatus.Success;
}
