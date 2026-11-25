namespace API.Controllers.Content;

using KanchanaburiPlatform.Application.Specifications.Content;

[ApiController]
[Route("api/tags")]
public sealed class TagsController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetTags()
    {
        var tags = (await unit.Repository<Tag>().ListAsync(
                new TagSearchSpecification(status: "Active")))
            .Select(ToDto)
            .ToList();
        return Ok(tags);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> GetTag(Guid id)
    {
        var tag = await unit.Repository<Tag>().GetEntityWithSpec(
            new TagSearchSpecification(tagId: id, status: "Active"));
        return tag is null ? NotFound() : Ok(ToDto(tag));
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<TagDto>>> GetTagsForAdmin(
        string? status, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var criteriaSpec = new TagSearchSpecification(status: status);
        var pageSpec = new TagSearchSpecification(status: status, page: page, pageSize: pageSize);
        var totalCount = await unit.Repository<Tag>().CountAsync(criteriaSpec);
        var tags = await unit.Repository<Tag>().ListAsync(pageSpec);
        return Ok(new PagedResultDto<TagDto>
        {
            Items = tags.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<TagDto>> GetTagForAdmin(Guid id) =>
        await unit.Repository<Tag>().GetEntityWithSpec(new TagSearchSpecification(tagId: id)) is { } tag ? Ok(ToDto(tag)) : NotFound();

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<TagDto>> CreateTag(CreateTagDto dto)
    {
        var name = dto.TagName.Trim();
        if (await TagNameExists(name)) return Conflict("A tag with this name already exists.");

        var tag = new Tag { TagId = Guid.NewGuid(), TagName = name, Status = "Active" };
        unit.Repository<Tag>().Add(tag);
        return await unit.Complete()
            ? CreatedAtAction(nameof(GetTagForAdmin), new { id = tag.TagId }, ToDto(tag))
            : BadRequest("Problem creating tag.");
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTag(Guid id, UpdateTagDto dto)
    {
        var tag = await unit.Repository<Tag>().GetEntityWithSpec(new TagSearchSpecification(tagId: id));
        if (tag is null) return NotFound();
        var name = dto.TagName.Trim();
        if (await TagNameExists(name, id)) return Conflict("A tag with this name already exists.");
        if (dto.Status is not ("Active" or "Inactive")) return BadRequest("Invalid tag status.");

        tag.TagName = name;
        tag.Status = dto.Status;
        unit.Repository<Tag>().Update(tag);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating tag.");
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTag(Guid id)
    {
        var tag = await unit.Repository<Tag>().GetEntityWithSpec(new TagSearchSpecification(tagId: id));
        if (tag is null) return NotFound();
        tag.Status = "Inactive";
        unit.Repository<Tag>().Update(tag);
        return await unit.Complete() ? NoContent() : BadRequest("Problem deleting tag.");
    }

    private async Task<bool> TagNameExists(string name, Guid? exceptId = null) =>
        await unit.Repository<Tag>().GetEntityWithSpec(
            new TagSearchSpecification(tagName: name, excludeTagId: exceptId)) is not null;

    private static TagDto ToDto(Tag tag) => new()
    {
        TagId = tag.TagId,
        TagName = tag.TagName,
        Status = tag.Status
    };
}
