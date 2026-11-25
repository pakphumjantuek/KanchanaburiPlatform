using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Content;

public sealed class ScheduleSearchSpecification : ISpecification<Schedule>
{
    public ScheduleSearchSpecification(
        Guid? scheduleId = null,
        Guid? contentId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? status = null,
        string? contentStatus = null,
        bool includeContent = false)
    {
        Criteria = schedule =>
            (!scheduleId.HasValue || schedule.ScheduleId == scheduleId.Value) &&
            (!contentId.HasValue || schedule.ContentId == contentId.Value) &&
            (!from.HasValue || schedule.StartDateTime >= from.Value) &&
            (!to.HasValue || schedule.StartDateTime <= to.Value) &&
            (status == null || schedule.Status == status) &&
            (contentStatus == null || schedule.Content.Status == contentStatus);

        Includes = includeContent ? [schedule => schedule.Content] : [];
        IncludeStrings = [];
        OrderBy = schedule => schedule.StartDateTime;
    }

    public Expression<Func<Schedule, bool>>? Criteria { get; }
    public Expression<Func<Schedule, object>>? OrderBy { get; }
    public Expression<Func<Schedule, object>>? OrderByDescending { get; }
    public List<Expression<Func<Schedule, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;

    public IQueryable<Schedule> ApplyCriteria(IQueryable<Schedule> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
