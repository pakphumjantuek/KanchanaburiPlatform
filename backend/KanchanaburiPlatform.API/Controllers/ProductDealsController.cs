namespace API.Controllers;

[ApiController]
[Route("api/product-deals")]
[Authorize]
public sealed class ProductDealsController(IProductDealService productDealService) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<ProductDealDto>>> GetMine()
    {
        var result = await productDealService.GetMineAsync(UserId());
        return ToActionResult(result);
    }

    [HttpGet("products/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ProductDealDto>>> GetByProduct(Guid productId)
    {
        var result = await productDealService.GetByProductAsync(productId, UserId());
        return ToActionResult(result);
    }

    [HttpPost("products/{productId:guid}")]
    public async Task<ActionResult<ProductDealDto>> Create(Guid productId, SaveProductDealDto dto)
    {
        var result = await productDealService.CreateAsync(productId, dto, UserId());
        return ToActionResult(result);
    }

    [HttpPut("{dealId:guid}")]
    public async Task<ActionResult<ProductDealDto>> Update(Guid dealId, SaveProductDealDto dto)
    {
        var result = await productDealService.UpdateAsync(dealId, dto, UserId());
        return ToActionResult(result);
    }

    [HttpPatch("{dealId:guid}/stop")]
    public async Task<IActionResult> Stop(Guid dealId)
    {
        var result = await productDealService.StopAsync(dealId, UserId());
        return result.Status == ServiceStatus.Success ? NoContent() : result.Status == ServiceStatus.NotFound ? NotFound() : result.Status == ServiceStatus.Forbidden ? Forbid() : BadRequest(result.Error);
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result) => result.Status == ServiceStatus.Success ? Ok(result.Data) : result.Status == ServiceStatus.NotFound ? NotFound() : result.Status == ServiceStatus.Forbidden ? Forbid() : BadRequest(result.Error);
}
