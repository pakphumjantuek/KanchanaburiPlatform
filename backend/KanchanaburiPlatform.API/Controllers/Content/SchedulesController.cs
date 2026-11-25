using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;
using KanchanaburiPlatform.Application.Specifications.Commerce;
using KanchanaburiPlatform.Application.Specifications.Content;

namespace API.Controllers.Content;

[ApiController]
[Route("api/schedules")]
public sealed class SchedulesController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScheduleDto>>> GetSchedules(
        Guid? contentId, DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && to < from) return BadRequest("The end date must not be before the start date.");

        var schedules = await unit.Repository<Schedule>().ListAsync(
            new ScheduleSearchSpecification(contentId: contentId, from: from, to: to, status: "Active", contentStatus: "Published", includeContent: true));
        var scheduleDtos = schedules.Select(schedule => ToDto(schedule, schedule.Content.Title)).ToList();
        return Ok(scheduleDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(Guid id)
    {
        var schedule = await unit.Repository<Schedule>().GetEntityWithSpec(
            new ScheduleSearchSpecification(scheduleId: id, status: "Active", contentStatus: "Published", includeContent: true));
        return schedule is null ? NotFound() : Ok(ToDto(schedule, schedule.Content.Title));
    }

    [HttpGet("mine"), Authorize]
    public async Task<ActionResult<IReadOnlyList<ScheduleDto>>> GetMySchedules(
        Guid contentId)
    {
        if (!await CanManageScheduleContent(contentId))
        {
            return Forbid();
        }

        var schedules = await unit.Repository<Schedule>().ListAsync(
            new ScheduleSearchSpecification(
                contentId: contentId,
                status: "Active",
                includeContent: true));

        return Ok(schedules
            .Select(schedule => ToDto(schedule, schedule.Content.Title))
            .ToList());
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ScheduleDto>>> GetSchedulesForAdmin(Guid? contentId)
    {
        var schedules = await unit.Repository<Schedule>().ListAsync(
            new ScheduleSearchSpecification(contentId: contentId, includeContent: true));
        return Ok(schedules.Select(schedule => ToDto(schedule, schedule.Content.Title)).ToList());
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ScheduleDto>> GetScheduleForAdmin(Guid id)
    {
        var schedule = await unit.Repository<Schedule>().GetEntityWithSpec(
            new ScheduleSearchSpecification(scheduleId: id, includeContent: true));
        return schedule is null ? NotFound() : Ok(ToDto(schedule, schedule.Content.Title));
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<ScheduleDto>> CreateSchedule(CreateScheduleDto dto)
    {
        var error = await Validate(dto);
        if (error is not null) return BadRequest(error);
        if (!await CanManageScheduleContent(dto.ContentId)) return Forbid();

        var schedule = new Schedule
        {
            ScheduleId = Guid.NewGuid(),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        CopyDto(dto, schedule);
        unit.Repository<Schedule>().Add(schedule);
        if (!await unit.Complete()) return BadRequest("Problem creating schedule.");

        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: schedule.ContentId));
        return CreatedAtAction(nameof(GetSchedule), new { id = schedule.ScheduleId }, ToDto(schedule, content!.Title));
    }

    [HttpPut("{id:guid}"), Authorize]
    public async Task<IActionResult> UpdateSchedule(Guid id, UpdateScheduleDto dto)
    {
        var schedule = await unit.Repository<Schedule>().GetEntityWithSpec(
            new ScheduleSearchSpecification(scheduleId: id));
        if (schedule is null) return NotFound();
        if (!await CanManageScheduleContent(schedule.ContentId)) return Forbid();
        if (!await CanManageScheduleContent(dto.ContentId)) return Forbid();
        var error = await Validate(dto);
        if (error is not null) return BadRequest(error);
        if (dto.Status is not ("Active" or "Inactive" or "Cancelled")) return BadRequest("Invalid schedule status.");

        CopyDto(dto, schedule);
        schedule.Status = dto.Status;
        unit.Repository<Schedule>().Update(schedule);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating schedule.");
    }

    [HttpDelete("{id:guid}"), Authorize]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var schedule = await unit.Repository<Schedule>().GetEntityWithSpec(
            new ScheduleSearchSpecification(scheduleId: id));
        if (schedule is null) return NotFound();
        if (!await CanManageScheduleContent(schedule.ContentId)) return Forbid();
        schedule.Status = "Inactive";
        unit.Repository<Schedule>().Update(schedule);
        return await unit.Complete() ? NoContent() : BadRequest("Problem archiving schedule.");
    }

    private async Task<bool> CanManageScheduleContent(Guid contentId)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: contentId));
        if (content is null) return false;
        var owner = content.OwnerShopId.HasValue
            ? await unit.Repository<Shop>().GetByIdAsync(content.OwnerShopId.Value) : null;
        return ContentOwnership.CanManage(content, User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole("Admin"), owner?.OwnerUserId);
    }

    private async Task<string?> Validate(SaveScheduleDto dto)
    {
        if (dto.EndDateTime.HasValue && dto.EndDateTime < dto.StartDateTime)
            return "The end date must not be before the start date.";
        return await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: dto.ContentId)) is null ? "Content not found." : null;
    }

    private static void CopyDto(SaveScheduleDto dto, Schedule schedule)
    {
        schedule.ContentId = dto.ContentId;
        schedule.Title = dto.Title.Trim();
        schedule.StartDateTime = dto.StartDateTime;
        schedule.EndDateTime = dto.EndDateTime;
        schedule.Address = dto.Address?.Trim();
        schedule.Latitude = dto.Latitude;
        schedule.Longitude = dto.Longitude;
        schedule.Description = dto.Description?.Trim();
    }

    private static ScheduleDto ToDto(Schedule schedule, string contentTitle) => new()
    {
        ScheduleId = schedule.ScheduleId,
        ContentId = schedule.ContentId,
        ContentTitle = contentTitle,
        Title = schedule.Title,
        StartDateTime = schedule.StartDateTime,
        EndDateTime = schedule.EndDateTime,
        Address = schedule.Address,
        Latitude = schedule.Latitude,
        Longitude = schedule.Longitude,
        Description = schedule.Description,
        Status = schedule.Status,
        CreatedAt = schedule.CreatedAt
    };
}
