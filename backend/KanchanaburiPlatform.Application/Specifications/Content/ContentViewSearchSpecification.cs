using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Content;

public sealed class ContentViewSearchSpecification : ISpecification<ContentView>
{
    public ContentViewSearchSpecification(
        string? userId = null,
        Guid? contentId = null,
        string? contentStatus = null,
        bool includeContent = false)
    {
        Criteria = view =>
            (string.IsNullOrWhiteSpace(userId) || view.UserId == userId) &&
            (!contentId.HasValue || view.ContentId == contentId.Value) &&
            (contentStatus == null || view.Content.Status == contentStatus);

        Includes = includeContent ? [view => view.Content] : [];
        IncludeStrings = [];
        OrderByDescending = view => view.ViewedAt;
    }

    public Expression<Func<ContentView, bool>>? Criteria { get; }
    public Expression<Func<ContentView, object>>? OrderBy { get; }
    public Expression<Func<ContentView, object>>? OrderByDescending { get; }
    public List<Expression<Func<ContentView, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;

    public IQueryable<ContentView> ApplyCriteria(IQueryable<ContentView> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
