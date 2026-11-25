namespace API.Controllers;

[ApiController]
[Route("api/shops")]
public sealed class ShopsController(IShopService shopService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ShopDto>>> GetShops(
        string? search,
        Guid? categoryId,
        Guid? districtId,
        Guid? subDistrictId,
        string? sortBy = "latest",
        int page = 1,
        int pageSize = 9)
    {
        var result = await shopService.GetPublicAsync(
            search, categoryId, districtId, subDistrictId, sortBy, page, pageSize);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShopDto>> GetShop(Guid id)
    {
        var shop = await shopService.GetAsync(id, activeOnly: true);
        return ToShopActionResult(shop, includeOwner: false);
    }

    [HttpGet("mine"), Authorize]
    public async Task<ActionResult<ShopDto>> GetMyShop()
    {
        var shop = await shopService.GetMineAsync(UserId());
        return ToShopActionResult(shop, includeOwner: true);
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<ShopDto>>> GetAllShopsForAdmin(
        string? search,
        string? status,
        int page = 1,
        int pageSize = 10)
    {
        var result = await shopService.GetAdminAsync(search, status, page, pageSize);
        return Ok(result);
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShopDto>> GetShopForAdmin(Guid id)
    {
        var shop = await shopService.GetAsync(id);
        return ToShopActionResult(shop, includeOwner: true);
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<ShopDto>> CreateShop(CreateShopDto dto)
    {
        var result = await shopService.CreateAsync(dto, UserId());

        return result.Status switch
        {
            ShopServiceStatus.Success => CreatedAtAction(
                nameof(GetShop),
                new { id = result.Shop!.ShopId },
                ToDto(result.Shop, includeOwner: true)),
            ShopServiceStatus.Conflict => Conflict(result.Error),
            _ => BadRequest(result.Error ?? "Problem creating shop")
        };
    }

    [HttpPut("{id:guid}"), Authorize]
    public async Task<IActionResult> UpdateShop(Guid id, UpdateShopDto dto)
    {
        var result = await shopService.UpdateAsync(id, dto, UserId(), IsAdmin);
        return ToNoContentActionResult(result);
    }

    [HttpPost("{id:guid}/cover-image"), Authorize]
    public async Task<ActionResult<ShopDto>> UploadCoverImage(Guid id, IFormFile file)
    {
        var result = await shopService.UploadImageAsync(id, file, UserId(), IsAdmin, isBackground: false);
        return ToShopServiceActionResult(result);
    }

    [HttpPost("{id:guid}/background-image"), Authorize]
    public async Task<ActionResult<ShopDto>> UploadBackgroundImage(Guid id, IFormFile file)
    {
        var result = await shopService.UploadImageAsync(id, file, UserId(), IsAdmin, isBackground: true);
        return ToShopServiceActionResult(result);
    }

    [HttpDelete("{id:guid}"), Authorize]
    public async Task<IActionResult> DeleteShop(Guid id)
    {
        var result = await shopService.CloseAsync(id, UserId(), IsAdmin);
        return ToNoContentActionResult(result);
    }

    [HttpPatch("{id:guid}/status"), Authorize]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateShopStatusDto dto)
    {
        var result = await shopService.UpdateStatusAsync(id, dto.Status, UserId(), IsAdmin);
        return ToNoContentActionResult(result);
    }

    [HttpGet("mine/dashboard"), Authorize]
    public async Task<ActionResult<MerchantDashboardReportDto>> GetMerchantDashboard()
    {
        var report = await shopService.GetDashboardAsync(UserId());
        return report is null ? NotFound("Shop not found") : Ok(report);
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool IsAdmin => User.IsInRole("Admin");

    private ActionResult<ShopDto> ToShopActionResult(Shop? shop, bool includeOwner)
    {
        return shop is null ? NotFound() : Ok(ToDto(shop, includeOwner));
    }

    private ActionResult<ShopDto> ToShopServiceActionResult(ShopServiceResult result)
    {
        return result.Status switch
        {
            ShopServiceStatus.Success => Ok(ToDto(result.Shop!, includeOwner: true)),
            ShopServiceStatus.NotFound => NotFound(),
            ShopServiceStatus.Forbidden => Forbid(),
            _ => BadRequest(result.Error ?? "Unable to update shop")
        };
    }

    private IActionResult ToNoContentActionResult(ShopServiceResult result)
    {
        return result.Status switch
        {
            ShopServiceStatus.Success => NoContent(),
            ShopServiceStatus.NotFound => NotFound(),
            ShopServiceStatus.Forbidden => Forbid(),
            _ => BadRequest(result.Error ?? "Unable to update shop")
        };
    }

    private static ShopDto ToDto(Shop shop, bool includeOwner = false) => new()
    {
        ShopId = shop.ShopId,
        OwnerUserId = includeOwner ? shop.OwnerUserId : null,
        ShopCategoryId = shop.ShopCategoryId,
        DistrictId = shop.DistrictId,
        SubDistrictId = shop.SubDistrictId,
        ShopName = shop.ShopName,
        Description = shop.Description,
        Phone = shop.Phone,
        Email = shop.Email,
        Address = shop.Address,
        CoverImageUrl = shop.CoverImageUrl,
        BackgroundImageUrl = shop.BackgroundImageUrl,
        OpeningTime = shop.OpeningTime,
        ClosingTime = shop.ClosingTime,
        Latitude = shop.Latitude,
        Longitude = shop.Longitude,
        Status = shop.Status,
        BankName = shop.BankName,
        BankAccountName = shop.BankAccountName,
        BankAccountNumber = shop.BankAccountNumber,
        PromptPay = shop.PromptPay,
        CreatedAt = shop.CreatedAt,
        UpdatedAt = shop.UpdatedAt,
        CategoryName = shop.ShopCategory?.CategoryName,
        DistrictName = shop.District?.DistrictName,
        SubDistrictName = shop.SubDistrict?.SubDistrictName
    };
}
