using Application.DTOs;
using Application.Results;

namespace Core.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<IReadOnlyList<OrderDto>>> CheckoutAsync(string userId, CheckoutDto dto);
    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(string userId);
    Task<ServiceResult<OrderDetailDto>> GetOrderAsync(Guid orderId, string? userId, bool isAdmin);
    Task<ServiceResult<IReadOnlyList<OrderDto>>> GetMyShopOrdersAsync(string userId, string? status, string? search, DateTime? startDate, DateTime? endDate);
    Task<ServiceResult<bool>> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, string? userId, bool isAdmin);
    Task<ServiceResult<OrderDto>> UploadSlipAsync(Guid orderId, UploadPaymentSlipFormDto dto, string? userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<ServiceResult<ShipmentDto>> UpdateShipmentAsync(Guid orderId, UpdateShipmentDto dto, string? userId, bool isAdmin);
    Task<ServiceResult<bool>> UpdateShipmentStatusAsync(Guid orderId, UpdateShipmentStatusDto dto, string? userId, bool isAdmin);
    Task<IReadOnlyList<AdminPendingSlipGroupDto>> GetPendingSlipsAsync();
    Task<ServiceResult<bool>> VerifySlipAsync(VerifySlipDto dto);
    Task<IReadOnlyList<AdminMerchantPayoutGroupDto>> GetPayoutsAsync();
    Task<ServiceResult<MerchantPayoutRecordDto>> ConfirmPayoutAsync(AdminConfirmPayoutFormDto dto);
    Task<IReadOnlyList<MerchantPayoutRecordDto>> GetPayoutHistoryAsync();
    Task<ServiceResult<MerchantPayoutSummaryDto>> GetMerchantPayoutsAsync(string userId);
}
