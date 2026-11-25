using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Content;

public sealed class ContentTagSearchSpecification : ISpecification<ContentTag>
{
    public ContentTagSearchSpecification(Guid? contentId = null, Guid? tagId = null, bool includeTag = false)
    {
        Criteria = contentTag =>
            (!contentId.HasValue || contentTag.ContentId == contentId.Value) &&
            (!tagId.HasValue || contentTag.TagId == tagId.Value);

        Includes = includeTag ? [contentTag => contentTag.Tag] : [];
        IncludeStrings = [];
        OrderBy = contentTag => contentTag.Tag.TagName;
    }

    public Expression<Func<ContentTag, bool>>? Criteria { get; }
    public Expression<Func<ContentTag, object>>? OrderBy { get; }
    public Expression<Func<ContentTag, object>>? OrderByDescending { get; }
    public List<Expression<Func<ContentTag, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;

    public IQueryable<ContentTag> ApplyCriteria(IQueryable<ContentTag> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
