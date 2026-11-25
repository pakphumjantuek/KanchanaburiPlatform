using Application.DTOs;
using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;
using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace Infrastructure.Services;

public sealed class ReportService(IUnitOfWork unit) : IReportService
{
    public async Task<ServiceResult<ReportDto>> CreateAsync(CreateReportDto dto, string userId)
    {
        if ((dto.ContentId.HasValue ? 1 : 0) + (dto.ReviewId.HasValue ? 1 : 0) != 1)
            return Invalid<ReportDto>("Select one item to report.");
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length > 300 || dto.Description?.Length > 2000)
            return Invalid<ReportDto>("Invalid report details.");
        if (dto.ContentId is { } contentId && await unit.Repository<ContentEntity>().GetByIdAsync(contentId) is not { Status: "Published" })
            return NotFound<ReportDto>();
        if (dto.ReviewId is { } reviewId && await unit.Repository<Review>().GetByIdAsync(reviewId) is not { Status: "Published" })
            return NotFound<ReportDto>();

        var exists = (await unit.Repository<Report>().ListAllAsync()).Any(report =>
            report.UserId == userId && report.ContentId == dto.ContentId && report.ReviewId == dto.ReviewId &&
            report.Status is "Pending" or "InProgress");
        if (exists)
            return ServiceResult<ReportDto>.Failure(ServiceStatus.Conflict, "You have already reported this item.");

        var report = new Report
        {
            ReportId = Guid.NewGuid(), UserId = userId, ContentId = dto.ContentId, ReviewId = dto.ReviewId,
            Reason = dto.Reason.Trim(), Description = dto.Description?.Trim(), Status = "Pending", CreatedAt = DateTime.UtcNow
        };
        unit.Repository<Report>().Add(report);
        return await unit.Complete()
            ? ServiceResult<ReportDto>.Success(await ToDtoAsync(report))
            : Invalid<ReportDto>("Problem creating report.");
    }

    public async Task<IReadOnlyList<ReportDto>> GetAllAsync(string? status)
    {
        var reports = (await unit.Repository<Report>().ListAllAsync())
            .Where(report => string.IsNullOrWhiteSpace(status) || report.Status == status)
            .OrderByDescending(report => report.CreatedAt);
        return await Task.WhenAll(reports.Select(ToDtoAsync));
    }

    public async Task<ServiceResult<ReportDto>> UpdateStatusAsync(Guid reportId, UpdateReportStatusDto dto)
    {
        if (dto.Status is not ("Pending" or "InProgress" or "Resolved" or "Dismissed"))
            return Invalid<ReportDto>("Invalid report status.");
        var report = await unit.Repository<Report>().GetByIdAsync(reportId);
        if (report is null)
            return NotFound<ReportDto>();

        report.Status = dto.Status;
        unit.Repository<Report>().Update(report);
        return await unit.Complete()
            ? ServiceResult<ReportDto>.Success(await ToDtoAsync(report))
            : Invalid<ReportDto>("Problem updating report.");
    }

    private async Task<ReportDto> ToDtoAsync(Report report)
    {
        var contentTitle = report.ContentId.HasValue
            ? (await unit.Repository<ContentEntity>().GetByIdAsync(report.ContentId.Value))?.Title : null;
        var review = report.ReviewId.HasValue ? await unit.Repository<Review>().GetByIdAsync(report.ReviewId.Value) : null;
        return new ReportDto
        {
            ReportId = report.ReportId, ContentId = report.ContentId, ReviewId = report.ReviewId,
            Reason = report.Reason, Description = report.Description, Status = report.Status, CreatedAt = report.CreatedAt,
            TargetLabel = contentTitle ?? (review is null ? "รายการที่ถูกรายงาน" : $"รีวิว: {review.Comment[..Math.Min(review.Comment.Length, 80)]}")
        };
    }

    private static ServiceResult<T> NotFound<T>() => ServiceResult<T>.Failure(ServiceStatus.NotFound);
    private static ServiceResult<T> Invalid<T>(string error) => ServiceResult<T>.Failure(ServiceStatus.Invalid, error);
}
