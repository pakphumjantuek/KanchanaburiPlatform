using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Commerce;

public sealed class CartItemSearchSpecification : ISpecification<CartItem>
{
    public CartItemSearchSpecification(Guid? cartItemId = null, Guid? cartId = null, Guid? productId = null)
    {
        Criteria = item =>
            (!cartItemId.HasValue || item.CartItemId == cartItemId.Value) &&
            (!cartId.HasValue || item.CartId == cartId.Value) &&
            (!productId.HasValue || item.ProductId == productId.Value);
        Includes = [];
        IncludeStrings = [];
    }

    public Expression<Func<CartItem, bool>>? Criteria { get; }
    public Expression<Func<CartItem, object>>? OrderBy => null;
    public Expression<Func<CartItem, object>>? OrderByDescending => null;
    public List<Expression<Func<CartItem, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;
    public IQueryable<CartItem> ApplyCriteria(IQueryable<CartItem> query) => Criteria is null ? query : query.Where(Criteria);
}
