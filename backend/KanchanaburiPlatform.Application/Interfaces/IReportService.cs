using Application.DTOs;
using Application.Results;

namespace Core.Interfaces;

public interface IReportService
{
    Task<ServiceResult<ReportDto>> CreateAsync(CreateReportDto dto, string userId);
    Task<IReadOnlyList<ReportDto>> GetAllAsync(string? status);
    Task<ServiceResult<ReportDto>> UpdateStatusAsync(Guid reportId, UpdateReportStatusDto dto);
}
