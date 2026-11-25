using Application.Results;

namespace Core.Interfaces;

public interface IAdminExportService
{
    Task<ServiceResult<ExportFileResult>> ExportOrdersAsync(DateTime? from, DateTime? to, Guid? shopId, string? orderStatus, string? paymentStatus, string? payoutStatus, bool pdf);
    Task<ServiceResult<ExportFileResult>> ExportShopSalesAsync(DateTime? from, DateTime? to, bool pdf);
    Task<ServiceResult<ExportFileResult>> ExportPayoutsAsync(DateTime? from, DateTime? to, Guid? shopId, bool pdf);
    Task<ServiceResult<ExportFileResult>> ExportPaymentSlipsAsync(DateTime? from, DateTime? to, string? paymentStatus, bool pdf);
}

public sealed record ExportFileResult(byte[] Content, string ContentType, string FileName);
