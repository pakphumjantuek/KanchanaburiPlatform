namespace API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    IProductService productService
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts()
    {
        var products = await productService.GetProductsAsync();
        return Ok(products);
    }

    [HttpGet("deals/active")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetActiveDealProducts(
        [FromQuery] int limit = 12
    )
    {
        var products = await productService.GetActiveDealProductsAsync(limit);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var result = await productService.GetProductAsync(id);
        return ToAction(result);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetProductsForAdmin(
        Guid? shopId,
        string? status,
        int page = 1,
        int pageSize = 10
    )
    {
        var result = await productService.GetProductsForAdminAsync(
            shopId,
            status,
            page,
            pageSize
        );

        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProductStatusForAdmin(
        Guid id,
        UpdateProductStatusDto dto
    )
    {
        var result = await productService.UpdateStatusAsync(
            id,
            dto
        );

        return ToNoContent(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        CreateProductDto dto
    )
    {
        var result = await productService.CreateAsync(
            dto,
            UserId(),
            IsAdmin
        );

        return ToCreated(
            result,
            nameof(GetProduct)
        );
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        UpdateProductDto dto
    )
    {
        var result = await productService.UpdateAsync(
            id,
            dto,
            UserId(),
            IsAdmin
        );

        return ToNoContent(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await productService.DeleteAsync(
            id,
            UserId(),
            IsAdmin
        );

        return ToNoContent(result);
    }

    [HttpPost("{id:guid}/cover-image")]
    [Authorize]
    public async Task<ActionResult<ProductDto>> UploadCoverImage(
        Guid id,
        IFormFile file
    )
    {
        var result = await productService.UploadCoverImageAsync(
            id,
            file,
            UserId(),
            IsAdmin,
            HttpContext.RequestAborted
        );

        return ToAction(result);
    }

    [HttpPost("{id:guid}/detail-images")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<string>>> UploadDetailImages(
        Guid id,
        List<IFormFile> files
    )
    {
        var result = await productService.UploadDetailImagesAsync(
            id,
            files,
            UserId(),
            IsAdmin,
            HttpContext.RequestAborted
        );

        return ToAction(result);
    }

    [HttpDelete("{productId:guid}/detail-images/{imageIndex:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteDetailImage(
        Guid productId,
        int imageIndex
    )
    {
        var result = await productService.DeleteDetailImageAsync(
            productId,
            imageIndex,
            UserId(),
            IsAdmin
        );

        return ToNoContent(result);
    }

    private string? UserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );
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

            default:
                return BadRequest(result.Error);
        }
    }

    private ActionResult<ProductDto> ToCreated(
        ServiceResult<ProductDto> result,
        string action
    )
    {
        switch (result.Status)
        {
            case ServiceStatus.Success:
                return CreatedAtAction(
                    action,
                    new
                    {
                        id = result.Data!.ProductId
                    },
                    result.Data
                );

            case ServiceStatus.NotFound:
                return NotFound(result.Error);

            case ServiceStatus.Forbidden:
                return Forbid();

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
}
