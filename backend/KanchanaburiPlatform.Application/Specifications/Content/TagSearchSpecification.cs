using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Content;

public sealed class TagSearchSpecification : ISpecification<Tag>
{
    public TagSearchSpecification(
        Guid? tagId = null,
        IReadOnlyCollection<Guid>? tagIds = null,
        string? status = null,
        string? tagName = null,
        Guid? excludeTagId = null,
        int? page = null,
        int pageSize = 20)
    {
        var normalizedTagName = tagName?.ToLower();

        Criteria = tag =>
            (!tagId.HasValue || tag.TagId == tagId.Value) &&
            (tagIds == null || tagIds.Contains(tag.TagId)) &&
            (status == null || tag.Status == status) &&
            (normalizedTagName == null || tag.TagName.ToLower() == normalizedTagName) &&
            (!excludeTagId.HasValue || tag.TagId != excludeTagId.Value);

        Includes = [];
        IncludeStrings = [];
        OrderBy = tag => tag.TagName;

        if (page.HasValue)
        {
            Skip = (page.Value - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
        }
    }

    public Expression<Func<Tag, bool>>? Criteria { get; }
    public Expression<Func<Tag, object>>? OrderBy { get; }
    public Expression<Func<Tag, object>>? OrderByDescending { get; }
    public List<Expression<Func<Tag, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take { get; }
    public int Skip { get; }
    public bool IsPagingEnabled { get; }

    public IQueryable<Tag> ApplyCriteria(IQueryable<Tag> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
