using Application.DTOs;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.AspNetCore.Http;

public interface IShopService
{
    Task<PagedResultDto<ShopDto>> GetPublicAsync(string? search, Guid? categoryId, Guid? districtId, Guid? subDistrictId, string? sortBy, int page, int pageSize);
    Task<Shop?> GetAsync(Guid id, bool activeOnly = false);
    Task<Shop?> GetMineAsync(string userId);
    Task<PagedResultDto<ShopDto>> GetAdminAsync(string? search, string? status, int page, int pageSize);
    Task<MerchantDashboardReportDto?> GetDashboardAsync(string userId);
    Task<ShopServiceResult> CreateAsync(CreateShopDto dto, string ownerUserId);
    Task<ShopServiceResult> UpdateAsync(Guid shopId, UpdateShopDto dto, string userId, bool isAdmin);
    Task<ShopServiceResult> UploadImageAsync(Guid shopId, IFormFile file, string userId, bool isAdmin, bool isBackground);
    Task<ShopServiceResult> CloseAsync(Guid shopId, string userId, bool isAdmin);
    Task<ShopServiceResult> UpdateStatusAsync(Guid shopId, string status, string userId, bool isAdmin);
}

public sealed record ShopServiceResult(ShopServiceStatus Status, Shop? Shop = null, string? Error = null);

public enum ShopServiceStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    Invalid,
    Failed
}