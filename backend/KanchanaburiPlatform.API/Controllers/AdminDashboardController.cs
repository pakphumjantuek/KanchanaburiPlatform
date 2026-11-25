namespace API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public sealed class AdminDashboardController(
    IAdminDashboardService dashboardService
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await dashboardService.GetDashboardAsync(from, to);

        if (result.Status != ServiceStatus.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }
}
