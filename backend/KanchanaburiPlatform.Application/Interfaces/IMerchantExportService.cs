using Application.Results;

namespace Core.Interfaces;

public interface IMerchantExportService
{
    Task<ServiceResult<ExportFileResult>> ExportOrdersAsync(string userId, DateTime? from, DateTime? to, string? orderStatus, bool pdf);
    Task<ServiceResult<ExportFileResult>> ExportProductsAsync(string userId, DateTime? from, DateTime? to, bool pdf);
    Task<ServiceResult<ExportFileResult>> ExportPayoutsAsync(string userId, DateTime? from, DateTime? to, bool pdf);
}
