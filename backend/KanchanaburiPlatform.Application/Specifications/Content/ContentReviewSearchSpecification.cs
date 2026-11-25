using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Content;

public sealed class ContentReviewSearchSpecification : ISpecification<Review>
{
    public ContentReviewSearchSpecification(
        Guid? contentId = null,
        string? userId = null,
        Guid? reviewId = null,
        string? status = null)
    {
        Criteria = review =>
            (!contentId.HasValue || review.ContentId == contentId.Value) &&
            (string.IsNullOrWhiteSpace(userId) || review.UserId == userId) &&
            (!reviewId.HasValue || review.ReviewId == reviewId.Value) &&
            (status == null || review.Status == status);

        Includes = [];
        IncludeStrings = [];
        OrderByDescending = review => review.CreatedAt;
    }

    public Expression<Func<Review, bool>>? Criteria { get; }
    public Expression<Func<Review, object>>? OrderBy { get; }
    public Expression<Func<Review, object>>? OrderByDescending { get; }
    public List<Expression<Func<Review, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;

    public IQueryable<Review> ApplyCriteria(IQueryable<Review> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
