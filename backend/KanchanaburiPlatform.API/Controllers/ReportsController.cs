namespace API.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpPost, Authorize]
    public async Task<ActionResult<ReportDto>> CreateReport(CreateReportDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return ToAction(await reportService.CreateAsync(dto, userId));
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ReportDto>>> GetReports(string? status)
    {
        return Ok(await reportService.GetAllAsync(status));
    }

    [HttpPatch("{reportId:guid}/status"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportDto>> UpdateStatus(Guid reportId, UpdateReportStatusDto dto)
    {
        return ToAction(await reportService.UpdateStatusAsync(reportId, dto));
    }

    private ActionResult<ReportDto> ToAction(ServiceResult<ReportDto> result) => result.Status switch { ServiceStatus.Success => Ok(result.Data), ServiceStatus.NotFound => NotFound(result.Error), ServiceStatus.Conflict => Conflict(result.Error), _ => BadRequest(result.Error) };
}
