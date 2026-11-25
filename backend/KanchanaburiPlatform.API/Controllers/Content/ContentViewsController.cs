using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;
using KanchanaburiPlatform.Application.Specifications.Content;

namespace API.Controllers.Content;

[ApiController, Authorize]
[Route("api/content-views")]
public sealed class ContentViewsController(IUnitOfWork unit) : ControllerBase
{
    [HttpPost("{contentId:guid}"), AllowAnonymous]
    public async Task<IActionResult> RecordView(Guid contentId)
    {
        if (await unit.Repository<ContentEntity>().GetEntityWithSpec(
                new ContentSearchSpecification(contentId: contentId)) is null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var trackingId = string.IsNullOrEmpty(userId)
            ? $"guest_{HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anon"}"
            : userId;

        var view = await unit.Repository<ContentView>().GetEntityWithSpec(
            new ContentViewSearchSpecification(trackingId, contentId));

        if (view is null)
        {
            unit.Repository<ContentView>().Add(new ContentView { ContentViewId = Guid.NewGuid(), ContentId = contentId, UserId = trackingId, ViewedAt = DateTime.UtcNow });
        }
        else
        {
            view.ViewedAt = DateTime.UtcNow;
            unit.Repository<ContentView>().Update(view);
        }
        return await unit.Complete() ? NoContent() : BadRequest("Problem recording content view.");
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<ContentViewHistoryDto>>> GetHistory(int take = 24)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        take = Math.Clamp(take, 1, 100);
        var recentViews = (await unit.Repository<ContentView>().ListAsync(
                new ContentViewSearchSpecification(userId, contentStatus: "Published", includeContent: true)))
            .GroupBy(item => item.ContentId)
            .Select(group => group.OrderByDescending(item => item.ViewedAt).First())
            .OrderByDescending(item => item.ViewedAt)
            .Take(take)
            .ToList();
        return Ok(recentViews
            .Select(view => new ContentViewHistoryDto
            {
                ContentId = view.ContentId,
                Title = view.Content.Title,
                Summary = view.Content.Summary,
                YoutubeUrl = view.Content.YoutubeUrl,
                ViewedAt = view.ViewedAt
            }).ToList());
    }
}

public sealed class ContentViewHistoryDto
{
    public Guid ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? YoutubeUrl { get; set; }
    public DateTime ViewedAt { get; set; }
}
