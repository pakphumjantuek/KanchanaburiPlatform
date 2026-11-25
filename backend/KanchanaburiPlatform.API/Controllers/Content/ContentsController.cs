using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;
using KanchanaburiPlatform.Application.Specifications.Content;

namespace API.Controllers.Content;

[ApiController]
[Route("api/contents")]
public sealed class ContentsController(IUnitOfWork unit, StoreContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetContents(
        Guid? categoryId, Guid? districtId, Guid? subDistrictId, Guid? tagId, Guid? shopId, string? search, string? sortBy, string? contentType = null, int page = 1, int pageSize = 20)
    {
        return Ok(await GetPagedContents(page, pageSize, (currentPage, size) =>
            new ContentSearchSpecification(categoryId, districtId, subDistrictId, tagId, search, page: currentPage, pageSize: size, shopId: shopId, sortBy: sortBy, contentType: contentType)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContentDto>> GetContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(contentId: id));
        if (content is null) return NotFound();
        var dto = ToDto(content);
        dto.ViewCount = await context.ContentViews.AsNoTracking().CountAsync(v => v.ContentId == id);
        return Ok(dto);
    }

    [HttpGet("{id:guid}/shop-products")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetShopProducts(Guid id, int take = 4)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(contentId: id));
        if (content is null) return NotFound();
        if (!content.ShopId.HasValue) return Ok(Array.Empty<ProductDto>());

        take = Math.Clamp(take, 1, 6);
        var products = (await unit.Repository<Product>().ListAllAsync())
            .Where(product => product.ShopId == content.ShopId.Value && product.Status == "Active")
            .OrderByDescending(product => product.UpdatedAt)
            .Take(take)
            .Select(product => new ProductDto
            {
                ProductId = product.ProductId,
                ShopId = product.ShopId,
                ShopName = content.Shop?.ShopName,
                ProductCategoryId = product.ProductCategoryId,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                ImageUrl = product.ImageUrl,
                Status = product.Status,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            }).ToList();
        return Ok(products);
    }

    [HttpGet("mine"), Authorize]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetMyContents(int page = 1, int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        return Ok(await GetPagedContents(page, pageSize, (currentPage, size) =>
            new ContentSearchSpecification(status: null, createdByUserId: userId, page: currentPage, pageSize: size, contentType: "User", includeUnpublished: true)));
    }

    [HttpGet("mine/{id:guid}"), Authorize]
    public async Task<ActionResult<ContentDto>> GetMyContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: id));
        return content is null ? NotFound() : content.ContentType == "User" && await CanManage(content) ? Ok(ToDto(content)) : Forbid();
    }

    [HttpGet("shop"), Authorize]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetShopContents(int page = 1, int pageSize = 20)
    {
        var shop = await GetOwnedShop();
        if (shop is null) return Forbid();
        return Ok(await GetPagedContents(page, pageSize, (currentPage, size) =>
            new ContentSearchSpecification(status: null, page: currentPage, pageSize: size,
                contentType: "Shop", ownerShopId: shop.ShopId, includeUnpublished: true)));
    }

    [HttpGet("shop/{id:guid}"), Authorize]
    public async Task<ActionResult<ContentDto>> GetOwnedShopContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: id));
        return content is null ? NotFound() : content.ContentType == "Shop" && await CanManage(content)
            ? Ok(ToDto(content)) : Forbid();
    }

    [HttpPost("shop"), Authorize]
    public async Task<ActionResult<ContentDto>> CreateShopContent(CreateContentDto dto)
    {
        var shop = await GetOwnedShop();
        if (shop is null || shop.Status != "Active") return Forbid();
        dto.ShopId = shop.ShopId;
        var error = await ValidateReferences(dto);
        if (error is not null) return BadRequest(error);
        var status = dto.Status is "Draft" or "Pending" or "Published" ? dto.Status : "Published";
        var content = new ContentEntity
        {
            ContentId = Guid.NewGuid(), ContentType = "Shop", OwnerShopId = shop.ShopId,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!, Status = status,
            PublishedAt = status == "Published" ? DateTime.UtcNow : null
        };
        CopyDto(dto, content);
        unit.Repository<ContentEntity>().Add(content);
        return await unit.Complete()
            ? CreatedAtAction(nameof(GetOwnedShopContent), new { id = content.ContentId }, ToDto(content))
            : BadRequest("Problem creating shop content.");
    }

    private async Task<Shop?> GetOwnedShop()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await context.Shops.FirstOrDefaultAsync(shop => shop.OwnerUserId == userId);
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetContentsForAdmin(
        string? status, string? search, Guid? categoryId, int page = 1, int pageSize = 20)
    {
        var effectiveStatus = string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase) ? null : status.Trim();
        var effectiveSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        return Ok(await GetPagedContents(page, pageSize, (currentPage, size) =>
            new ContentSearchSpecification(categoryId: categoryId, searchTerm: effectiveSearch, status: effectiveStatus, page: currentPage, pageSize: size)));
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContentDto>> GetContentForAdmin(Guid id) =>
        await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: id)) is { } content ? Ok(ToDto(content)) : NotFound();

    [HttpPost, Authorize]
    public async Task<ActionResult<ContentDto>> CreateContent(CreateContentDto dto)
    {
        if (string.IsNullOrWhiteSpace(User.FindFirstValue(ClaimTypes.NameIdentifier))) return Unauthorized();
        var error = await ValidateReferences(dto);
        if (error is not null) return BadRequest(error);
        if (!await CanUseShop(dto.ShopId)) return BadRequest("Shop is not available.");

        var initialStatus = string.IsNullOrWhiteSpace(dto.Status) ? "Published" : dto.Status;
        if (initialStatus is not ("Draft" or "Pending" or "Published")) initialStatus = "Published";

        var content = new ContentEntity
        {
            ContentId = Guid.NewGuid(),
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Status = initialStatus,
            PublishedAt = initialStatus == "Published" ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        CopyDto(dto, content);
        unit.Repository<ContentEntity>().Add(content);
        return await unit.Complete()
            ? CreatedAtAction(nameof(GetMyContent), new { id = content.ContentId }, ToDto(content))
            : BadRequest("Problem creating content.");
    }

    [HttpPut("{id:guid}"), Authorize]
    public async Task<IActionResult> UpdateContent(Guid id, UpdateContentDto dto)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(id);
        if (content is null) return NotFound();
        if (!await CanManage(content)) return Forbid();
        var error = await ValidateReferences(dto);
        if (error is not null) return BadRequest(error);
        if (!await CanUseShop(dto.ShopId)) return BadRequest("Shop is not available.");
        if (content.ContentType == "Shop" && dto.ShopId != content.OwnerShopId)
            return BadRequest("Shop content must remain attached to its owning shop.");
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && content.Status == "Archived")
            return BadRequest("Archived content cannot be edited.");
        if (dto.Status is not ("Draft" or "Pending" or "Published" or "Archived")) return BadRequest("Invalid content status.");

        CopyDto(dto, content);
        content.Status = dto.Status;
        content.PublishedAt = dto.Status == "Published" ? content.PublishedAt ?? DateTime.UtcNow : null;
        content.UpdatedAt = DateTime.UtcNow;
        unit.Repository<ContentEntity>().Update(content);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating content.");
    }

    [HttpDelete("{id:guid}"), Authorize]
    public async Task<IActionResult> DeleteContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(id);
        if (content is null) return NotFound();
        if (!await CanManage(content)) return Forbid();
        content.Status = "Archived";
        content.UpdatedAt = DateTime.UtcNow;
        unit.Repository<ContentEntity>().Update(content);
        return await unit.Complete() ? NoContent() : BadRequest("Problem archiving content.");
    }

    [HttpPost("{id:guid}/restore"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(id);
        if (content is null) return NotFound();
        content.Status = "Published";
        content.PublishedAt ??= DateTime.UtcNow;
        content.UpdatedAt = DateTime.UtcNow;
        unit.Repository<ContentEntity>().Update(content);
        return await unit.Complete() ? NoContent() : BadRequest("Problem restoring content.");
    }

    private async Task<string?> ValidateReferences(SaveContentDto dto)
    {
        var category = await unit.Repository<ContentCategory>().GetByIdAsync(dto.ContentCategoryId);
        if (category is null || category.Status != "Active") return "Content category is not available.";
        if (dto.DistrictId is { } districtId && await unit.Repository<District>().GetByIdAsync(districtId) is null)
            return "District not found.";
        if (dto.SubDistrictId is { } subDistrictId)
        {
            var subDistrict = await unit.Repository<SubDistrict>().GetByIdAsync(subDistrictId);
            if (subDistrict is null || (dto.DistrictId.HasValue && subDistrict.DistrictId != dto.DistrictId.Value))
                return "Sub-district does not belong to the selected district.";
        }
        return null;
    }
    private async Task<bool> CanUseShop(Guid? shopId)
    {
        if (!shopId.HasValue) return true;
        var shop = await unit.Repository<Shop>().GetByIdAsync(shopId.Value);
        return shop is not null && shop.Status == "Active";
    }
    private async Task<bool> CanManage(ContentEntity content)
    {
        var owner = content.OwnerShopId.HasValue
            ? await unit.Repository<Shop>().GetByIdAsync(content.OwnerShopId.Value) : null;
        return ContentOwnership.CanManage(content, User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole("Admin"), owner?.OwnerUserId);
    }
    private async Task<PagedResultDto<ContentDto>> GetPagedContents(
        int page, int pageSize, Func<int?, int, ContentSearchSpecification> createSpecification)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await unit.Repository<ContentEntity>().CountAsync(createSpecification(null, pageSize));
        var contents = await unit.Repository<ContentEntity>().ListAsync(createSpecification(page, pageSize));

        var dtos = contents.Select(ToDto).ToList();
        var contentIds = dtos.Select(x => x.ContentId).ToList();
        if (contentIds.Count > 0)
        {
            var viewCounts = await context.ContentViews
                .AsNoTracking()
                .Where(v => contentIds.Contains(v.ContentId))
                .GroupBy(v => v.ContentId)
                .Select(g => new { ContentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ContentId, x => x.Count);

            foreach (var dto in dtos)
            {
                if (viewCounts.TryGetValue(dto.ContentId, out var count))
                {
                    dto.ViewCount = count;
                }
            }
        }

        return new PagedResultDto<ContentDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
    private static void CopyDto(SaveContentDto dto, ContentEntity content)
    {
        content.Title = dto.Title.Trim();
        content.Summary = dto.Summary?.Trim();
        content.ContentCategoryId = dto.ContentCategoryId;
        content.ShopId = dto.ShopId;
        content.DistrictId = dto.DistrictId;
        content.SubDistrictId = dto.SubDistrictId;
        content.Latitude = dto.Latitude;
        content.Longitude = dto.Longitude;
        content.YoutubeUrl = dto.YoutubeUrl?.Trim();
    }
    private static ContentDto ToDto(ContentEntity content) => new()
    {
        ContentId = content.ContentId,
        CreatedByUserId = content.CreatedByUserId,
        ContentType = content.ContentType,
        OwnerShopId = content.OwnerShopId,
        ShopId = content.ShopId,
        ShopName = content.Shop?.ShopName,
        ContentCategoryId = content.ContentCategoryId,
        ContentCategoryName = content.ContentCategory?.CategoryName,
        DistrictId = content.DistrictId,
        DistrictName = content.District?.DistrictName,
        SubDistrictId = content.SubDistrictId,
        SubDistrictName = content.SubDistrict?.SubDistrictName,
        Title = content.Title,
        Summary = content.Summary,
        Latitude = content.Latitude,
        Longitude = content.Longitude,
        YoutubeUrl = content.YoutubeUrl,
        Status = content.Status,
        PublishedAt = content.PublishedAt,
        CreatedAt = content.CreatedAt,
        UpdatedAt = content.UpdatedAt,
        ViewCount = content.Views?.Count ?? 0,
        Tags = content.ContentTags.Select(contentTag => new TagDto
        {
            TagId = contentTag.TagId,
            TagName = contentTag.Tag.TagName,
            Status = contentTag.Tag.Status
        }).ToList()
    };

}
