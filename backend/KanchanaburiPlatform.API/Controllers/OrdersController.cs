namespace API.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    IOrderService orderService
) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Checkout(
        CheckoutDto dto
    )
    {
        return ToAction(
            await orderService.CheckoutAsync(
                UserId(),
                dto
            )
        );
    }

    [HttpGet("mine")]
    public Task<IReadOnlyList<OrderDto>> MyOrders()
    {
        return orderService.GetMyOrdersAsync(
            UserId()
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(
        Guid id
    )
    {
        return ToAction(
            await orderService.GetOrderAsync(
                id,
                UserId(),
                IsAdmin
            )
        );
    }

    [HttpGet("shop/mine")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> MyShopOrders(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate
    )
    {
        return ToAction(
            await orderService.GetMyShopOrdersAsync(
                UserId(),
                status,
                search,
                startDate,
                endDate
            )
        );
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        UpdateOrderStatusDto dto
    )
    {
        return ToNoContent(
            await orderService.UpdateStatusAsync(
                id,
                dto,
                UserId(),
                IsAdmin
            )
        );
    }

    [HttpPost("{id:guid}/upload-slip")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<OrderDto>> UploadSlip(
        Guid id,
        [FromForm] UploadPaymentSlipFormDto dto
    )
    {
        return ToAction(
            await orderService.UploadSlipAsync(
                id,
                dto,
                UserId(),
                IsAdmin,
                HttpContext.RequestAborted
            )
        );
    }

    [HttpPut("{id:guid}/shipment")]
    public async Task<ActionResult<ShipmentDto>> UpdateShipment(
        Guid id,
        UpdateShipmentDto dto
    )
    {
        return ToAction(
            await orderService.UpdateShipmentAsync(
                id,
                dto,
                UserId(),
                IsAdmin
            )
        );
    }

    [HttpPatch("{id:guid}/shipment/status")]
    public async Task<IActionResult> UpdateShipmentStatus(
        Guid id,
        UpdateShipmentStatusDto dto
    )
    {
        return ToNoContent(
            await orderService.UpdateShipmentStatusAsync(
                id,
                dto,
                UserId(),
                IsAdmin
            )
        );
    }

    [HttpGet("admin/pending-slips")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AdminPendingSlipGroupDto>>> GetAdminPendingSlips()
    {
        var result = await orderService.GetPendingSlipsAsync();

        return Ok(result);
    }

    [HttpPost("admin/verify-slip")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VerifySlip(
        VerifySlipDto dto
    )
    {
        return ToOk(
            await orderService.VerifySlipAsync(dto)
        );
    }

    [HttpGet("admin/payouts")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AdminMerchantPayoutGroupDto>>> GetAdminPayouts()
    {
        var result = await orderService.GetPayoutsAsync();

        return Ok(result);
    }

    [HttpPost("admin/payouts/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MerchantPayoutRecordDto>> ConfirmPayout(
        [FromForm] AdminConfirmPayoutFormDto dto
    )
    {
        return ToAction(
            await orderService.ConfirmPayoutAsync(dto)
        );
    }

    [HttpGet("admin/payouts/history")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<MerchantPayoutRecordDto>>> GetAdminPayoutHistory()
    {
        var result = await orderService.GetPayoutHistoryAsync();

        return Ok(result);
    }

    [HttpGet("merchant/payouts")]
    [HttpGet("/api/shops/mine/payouts")]
    public async Task<ActionResult<MerchantPayoutSummaryDto>> GetMerchantPayouts()
    {
        return ToAction(
            await orderService.GetMerchantPayoutsAsync(
                UserId()
            )
        );
    }

    private string UserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier
        ) ?? string.Empty;
    }

    private bool IsAdmin
    {
        get
        {
            return User.IsInRole("Admin");
        }
    }

    private ActionResult<T> ToAction<T>(
        ServiceResult<T> result
    )
    {
        switch (result.Status)
        {
            case ServiceStatus.Success:
                return Ok(result.Data);

            case ServiceStatus.NotFound:
                return NotFound(result.Error);

            case ServiceStatus.Forbidden:
                return Forbid();

            case ServiceStatus.Conflict:
                return Conflict(result.Error);

            default:
                return BadRequest(result.Error);
        }
    }

    private IActionResult ToNoContent(
        ServiceResult<bool> result
    )
    {
        switch (result.Status)
        {
            case ServiceStatus.Success:
                return NoContent();

            case ServiceStatus.NotFound:
                return NotFound(result.Error);

            case ServiceStatus.Forbidden:
                return Forbid();

            default:
                return BadRequest(result.Error);
        }
    }

    private IActionResult ToOk(
        ServiceResult<bool> result
    )
    {
        switch (result.Status)
        {
            case ServiceStatus.Success:
                return Ok();

            case ServiceStatus.NotFound:
                return NotFound(result.Error);

            case ServiceStatus.Forbidden:
                return Forbid();

            default:
                return BadRequest(result.Error);
        }
    }
}