namespace API.Controllers;

[ApiController]
[Route("api/shops/mine/exports")]
public sealed class MerchantExportsController(
    IMerchantExportService exportService
) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? orderStatus)
    {
        var result = await exportService.ExportOrdersAsync(
            UserId(),
            from,
            to,
            orderStatus,
            IsPdf);

        return ToFile(result);
    }

    [HttpGet("products")]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await exportService.ExportProductsAsync(
            UserId(),
            from,
            to,
            IsPdf);

        return ToFile(result);
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> ExportPayouts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await exportService.ExportPayoutsAsync(
            UserId(),
            from,
            to,
            IsPdf);

        return ToFile(result);
    }

    private string UserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private bool IsPdf
    {
        get
        {
            return string.Equals(
                Request.Query["format"],
                "pdf",
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private IActionResult ToFile(ServiceResult<ExportFileResult> result)
    {
        if (result.Status == ServiceStatus.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Status != ServiceStatus.Success)
        {
            return BadRequest(result.Error);
        }

        var file = result.Data!;

        return File(file.Content, file.ContentType, file.FileName);
    }
}
