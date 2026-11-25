using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Commerce;

public sealed class ProductCategorySearchSpecification : ISpecification<ProductCategory>
{
    public ProductCategorySearchSpecification(Guid? categoryId = null, string? status = null, int? page = null, int pageSize = 20)
    {
        Criteria = category =>
            (!categoryId.HasValue || category.ProductCategoryId == categoryId.Value) &&
            (status == null || category.Status == status);
        Includes = [];
        IncludeStrings = [];
        OrderBy = category => category.CategoryName;

        if (page.HasValue)
        {
            Skip = (page.Value - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
        }
    }

    public Expression<Func<ProductCategory, bool>>? Criteria { get; }
    public Expression<Func<ProductCategory, object>>? OrderBy { get; }
    public Expression<Func<ProductCategory, object>>? OrderByDescending => null;
    public List<Expression<Func<ProductCategory, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take { get; }
    public int Skip { get; }
    public bool IsPagingEnabled { get; }
    public IQueryable<ProductCategory> ApplyCriteria(IQueryable<ProductCategory> query) => Criteria is null ? query : query.Where(Criteria);
}
