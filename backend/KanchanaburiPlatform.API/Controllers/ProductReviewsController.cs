namespace API.Controllers;

[ApiController]
[Route("api/product-reviews")]
public sealed class ProductReviewsController(
    IProductReviewService productReviewService
) : ControllerBase
{
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductReviewsDto>> GetReviews(Guid productId)
    {
        var result = await productReviewService.GetReviewsAsync(productId);

        if (result.Status == ServiceStatus.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Status != ServiceStatus.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }

    [HttpGet("my-reviews")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetMyReviewedProductIds(
        [FromQuery] Guid orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var productIds = await productReviewService
            .GetReviewedProductIdsAsync(userId, orderId);

        return Ok(productIds);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProductReviewDto>> CreateReview(
        [FromBody] CreateProductReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await productReviewService.CreateAsync(dto, userId);

        if (result.Status == ServiceStatus.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Status != ServiceStatus.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Data);
    }
}
