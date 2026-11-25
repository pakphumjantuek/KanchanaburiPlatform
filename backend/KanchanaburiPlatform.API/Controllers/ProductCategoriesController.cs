namespace API.Controllers;

[ApiController]
[Route("api/product-categories")]
public sealed class ProductCategoriesController(
    IProductCategoryService productCategoryService
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductCategoryDto>>> GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var categories = await productCategoryService.GetCategoriesAsync(
            page,
            pageSize);

        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductCategoryDto>> GetCategory(Guid id)
    {
        var result = await productCategoryService.GetCategoryAsync(id);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductCategoryDto>> CreateCategory(
        CreateProductCategoryDto dto)
    {
        var result = await productCategoryService.CreateAsync(dto);

        if (result.Status == ServiceStatus.Success)
        {
            return CreatedAtAction(
                nameof(GetCategory),
                new { id = result.Data!.ProductCategoryId },
                result.Data);
        }

        return BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        UpdateProductCategoryDto dto)
    {
        var result = await productCategoryService.UpdateAsync(id, dto);

        return ToNoContentResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = await productCategoryService.DeleteAsync(id);

        return ToNoContentResult(result);
    }

    private ActionResult<ProductCategoryDto> ToActionResult(
        ServiceResult<ProductCategoryDto> result)
    {
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

    private IActionResult ToNoContentResult(ServiceResult<bool> result)
    {
        if (result.Status == ServiceStatus.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Status != ServiceStatus.Success)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
