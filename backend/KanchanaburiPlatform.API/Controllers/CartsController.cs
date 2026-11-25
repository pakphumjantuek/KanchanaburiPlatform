namespace API.Controllers;

[ApiController, Authorize]
[Route("api/cart")]
public sealed class CartsController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        return Ok(await cartService.GetCartAsync(UserId()));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemDto dto)
    {
        var result = await cartService.AddItemAsync(UserId(), dto);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Message);
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, UpdateCartItemDto dto)
    {
        return ToActionResult(await cartService.UpdateItemAsync(UserId(), id, dto));
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        return ToActionResult(await cartService.RemoveItemAsync(UserId(), id));
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IActionResult ToActionResult(CartOperationResult result) => result.Status switch
    {
        CartOperationStatus.Success => NoContent(),
        CartOperationStatus.NotFound => NotFound(),
        CartOperationStatus.Forbidden => Forbid(),
        _ => BadRequest(result.Message)
    };
}
