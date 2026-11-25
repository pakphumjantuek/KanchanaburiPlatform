using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Content;

public sealed class ContentFavoriteSearchSpecification : ISpecification<ContentFavorite>
{
    public ContentFavoriteSearchSpecification(
        string userId,
        Guid? contentId = null,
        string? contentStatus = null,
        bool includeContent = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        Criteria = favorite =>
            favorite.UserId == userId &&
            (!contentId.HasValue || favorite.ContentId == contentId.Value) &&
            (contentStatus == null || favorite.Content.Status == contentStatus);

        Includes = includeContent ? [favorite => favorite.Content] : [];
        IncludeStrings = [];
        OrderByDescending = favorite => favorite.CreatedAt;
    }

    public Expression<Func<ContentFavorite, bool>>? Criteria { get; }
    public Expression<Func<ContentFavorite, object>>? OrderBy { get; }
    public Expression<Func<ContentFavorite, object>>? OrderByDescending { get; }
    public List<Expression<Func<ContentFavorite, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;

    public IQueryable<ContentFavorite> ApplyCriteria(IQueryable<ContentFavorite> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
