using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Commerce;

public sealed class ProductSearchSpecification : ISpecification<Product>
{
    public ProductSearchSpecification(Guid? productId = null, Guid? shopId = null, string? status = null, int? page = null, int pageSize = 20)
    {
        Criteria = product => (!productId.HasValue || product.ProductId == productId.Value) && (!shopId.HasValue || product.ShopId == shopId.Value) && (status == null || product.Status == status);
        Includes = []; IncludeStrings = []; OrderByDescending = product => product.UpdatedAt;
        if (page.HasValue) { Skip = (page.Value - 1) * pageSize; Take = pageSize; IsPagingEnabled = true; }
    }
    public Expression<Func<Product, bool>>? Criteria { get; }
    public Expression<Func<Product, object>>? OrderBy => null;
    public Expression<Func<Product, object>>? OrderByDescending { get; }
    public List<Expression<Func<Product, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take { get; }
    public int Skip { get; }
    public bool IsPagingEnabled { get; }
    public IQueryable<Product> ApplyCriteria(IQueryable<Product> query) => Criteria is null ? query : query.Where(Criteria);
}
