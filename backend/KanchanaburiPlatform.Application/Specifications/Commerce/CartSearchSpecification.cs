using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Commerce;

public sealed class CartSearchSpecification : ISpecification<Cart>
{
    public CartSearchSpecification(string? userId = null, Guid? cartId = null, bool includeItems = false)
    {
        Criteria = cart =>
            (string.IsNullOrWhiteSpace(userId) || cart.UserId == userId) &&
            (!cartId.HasValue || cart.CartId == cartId.Value);
        Includes = [];
        IncludeStrings = includeItems ? ["Items.Product.Shop"] : [];
    }

    public Expression<Func<Cart, bool>>? Criteria { get; }
    public Expression<Func<Cart, object>>? OrderBy => null;
    public Expression<Func<Cart, object>>? OrderByDescending => null;
    public List<Expression<Func<Cart, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;
    public IQueryable<Cart> ApplyCriteria(IQueryable<Cart> query) => Criteria is null ? query : query.Where(Criteria);
}
