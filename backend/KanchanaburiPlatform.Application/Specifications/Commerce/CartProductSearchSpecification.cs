using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Commerce;

public sealed class CartProductSearchSpecification : ISpecification<Product>
{
    public CartProductSearchSpecification(Guid productId, string? status = null)
    {
        Criteria = product => product.ProductId == productId && (status == null || product.Status == status);
        Includes = [];
        IncludeStrings = [];
    }

    public Expression<Func<Product, bool>>? Criteria { get; }
    public Expression<Func<Product, object>>? OrderBy => null;
    public Expression<Func<Product, object>>? OrderByDescending => null;
    public List<Expression<Func<Product, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;
    public IQueryable<Product> ApplyCriteria(IQueryable<Product> query) => Criteria is null ? query : query.Where(Criteria);
}
