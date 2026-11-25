namespace API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(
    IPaymentService paymentService
) : ControllerBase
{
    [HttpPost("orders/{orderId:guid}/intent")]
    [Authorize]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreateIntent(
        Guid orderId
    )
    {
        return ToAction(
            await paymentService.CreateIntentAsync(
                orderId,
                UserId()
            )
        );
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(
            HttpContext.Request.Body
        );

        var payload = await reader.ReadToEndAsync();

        var signature = Request.Headers[
            "Stripe-Signature"
        ];

        var result = await paymentService.ProcessWebhookAsync(
            payload,
            signature
        );

        return ToOk(result);
    }

    [HttpPost("orders/{orderId:guid}/sync")]
    [Authorize]
    public async Task<IActionResult> SyncPayment(
        Guid orderId
    )
    {
        return ToOk(
            await paymentService.SyncPaymentAsync(
                orderId,
                UserId()
            )
        );
    }

    [HttpGet("orders/summary")]
    [Authorize]
    public async Task<ActionResult<BatchOrderSummaryDto>> GetOrdersSummary(
        [FromQuery] string orderIds
    )
    {
        return ToAction(
            await paymentService.GetOrdersSummaryAsync(
                orderIds,
                UserId()
            )
        );
    }

    [HttpPost("batch/intent")]
    [Authorize]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreateBatchIntent(
        BatchPaymentRequestDto dto
    )
    {
        return ToAction(
            await paymentService.CreateBatchIntentAsync(
                dto,
                UserId()
            )
        );
    }

    [HttpPost("batch/sync")]
    [Authorize]
    public async Task<IActionResult> SyncBatchPayment(
        BatchPaymentRequestDto dto
    )
    {
        return ToOk(
            await paymentService.SyncBatchPaymentAsync(
                dto,
                UserId()
            )
        );
    }

    private string? UserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );
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

            case ServiceStatus.Failed:
                return Problem(
                    result.Error,
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable
                );

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

            case ServiceStatus.Failed:
                return Problem(
                    result.Error,
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable
                );

            default:
                return BadRequest(result.Error);
        }
    }
}