using Application.DTOs;
using Application.Results;

namespace Core.Interfaces;

public interface IPaymentService
{
    Task<ServiceResult<CreatePaymentIntentResponse>> CreateIntentAsync(Guid orderId, string? userId);
    Task<ServiceResult<bool>> ProcessWebhookAsync(string json, string? signature);
    Task<ServiceResult<bool>> SyncPaymentAsync(Guid orderId, string? userId);
    Task<ServiceResult<BatchOrderSummaryDto>> GetOrdersSummaryAsync(string orderIds, string? userId);
    Task<ServiceResult<CreatePaymentIntentResponse>> CreateBatchIntentAsync(BatchPaymentRequestDto dto, string? userId);
    Task<ServiceResult<bool>> SyncBatchPaymentAsync(BatchPaymentRequestDto dto, string? userId);
}
