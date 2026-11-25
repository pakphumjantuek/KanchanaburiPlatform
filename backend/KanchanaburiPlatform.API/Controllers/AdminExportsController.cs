namespace API.Controllers;

[ApiController]
[Route("api/admin/exports")]
[Authorize(Roles = "Admin")]
public sealed class AdminExportsController(IAdminExportService exportService) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? shopId,
        [FromQuery] string? orderStatus,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? payoutStatus)
    {
        var result = await exportService.ExportOrdersAsync(
            from,
            to,
            shopId,
            orderStatus,
            paymentStatus,
            payoutStatus,
            IsPdf);

        return ToFile(result);
    }

    [HttpGet("shop-sales")]
    public async Task<IActionResult> ExportShopSales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await exportService.ExportShopSalesAsync(from, to, IsPdf);

        return ToFile(result);
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> ExportPayouts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? shopId)
    {
        var result = await exportService.ExportPayoutsAsync(
            from,
            to,
            shopId,
            IsPdf);

        return ToFile(result);
    }

    [HttpGet("payment-slips")]
    public async Task<IActionResult> ExportPaymentSlips(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? paymentStatus)
    {
        var result = await exportService.ExportPaymentSlipsAsync(
            from,
            to,
            paymentStatus,
            IsPdf);

        return ToFile(result);
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
        if (result.Status != ServiceStatus.Success)
        {
            return BadRequest(result.Error);
        }

        var file = result.Data!;

        return File(file.Content, file.ContentType, file.FileName);
    }
}
