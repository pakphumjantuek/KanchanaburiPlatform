using Application.Constants;
using Infrastructure.Services;
using Application.DTOs;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.AspNetCore.Http;
using KanchanaburiPlatform.Application.Specifications.Commerce;
using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;
namespace Infrastructure.Services;


public sealed class ShopService(
    IUnitOfWork unit,
    ShopBusinessService business,
    IImageUploadService imageUploadService) : IShopService
{
    public async Task<PagedResultDto<ShopDto>> GetPublicAsync(string? search, Guid? categoryId, Guid? districtId, Guid? subDistrictId, string? sortBy, int page, int pageSize)
    {
        var criteria = new ShopSearchSpecification(search, categoryId, districtId, subDistrictId, ShopStatuses.Active);
        var pageSpecification = new ShopSearchSpecification(search, categoryId, districtId, subDistrictId, ShopStatuses.Active, sortBy: sortBy, page: page, pageSize: pageSize);
        return await GetPagedAsync(criteria, pageSpecification, page, pageSize, includeOwner: false);
    }

    public async Task<Shop?> GetAsync(Guid id, bool activeOnly = false)
    {
        var status = activeOnly ? ShopStatuses.Active : null;
        return await unit.Repository<Shop>().GetEntityWithSpec(new ShopSearchSpecification(shopId: id, status: status));
    }

    public async Task<Shop?> GetMineAsync(string userId)
    {
        return await unit.Repository<Shop>().GetEntityWithSpec(new ShopSearchSpecification(ownerUserId: userId));
    }

    public async Task<PagedResultDto<ShopDto>> GetAdminAsync(string? search, string? status, int page, int pageSize)
    {
        var criteria = new ShopSearchSpecification(search: search, status: status);
        var pageSpecification = new ShopSearchSpecification(search: search, status: status, page: page, pageSize: pageSize);
        return await GetPagedAsync(criteria, pageSpecification, page, pageSize, includeOwner: true);
    }

    private async Task<PagedResultDto<ShopDto>> GetPagedAsync(ShopSearchSpecification criteria, ShopSearchSpecification pageSpecification, int page, int pageSize, bool includeOwner)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var shops = await unit.Repository<Shop>().ListAsync(pageSpecification);
        var total = await unit.Repository<Shop>().CountAsync(criteria);
        return new PagedResultDto<ShopDto>
        {
            Items = shops.Select(shop => ToDto(shop, includeOwner)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
    public async Task<MerchantDashboardReportDto?> GetDashboardAsync(string userId)
    {
        var shop = await GetMineAsync(userId);
        if (shop is null) return null;
        var now = DateTime.UtcNow;
        var today = now.Date;
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var orders = (await unit.Repository<Order>().ListAsync(x => x.ShopId == shop.ShopId)).OrderByDescending(x => x.CreatedAt).ToList();
        var orderIds = orders.Select(x => x.OrderId).ToHashSet();
        var items = await unit.Repository<OrderItem>().ListAsync(x => orderIds.Contains(x.OrderId));
        var products = await unit.Repository<Product>().ListAsync(x => x.ShopId == shop.ShopId && x.Status != "Archived");
        var contents = await unit.Repository<ContentEntity>().ListAsync(x => x.ShopId == shop.ShopId && x.Status != "Archived");
        var reviews = (await unit.Repository<Review>().ListAsync(x => x.ShopId == shop.ShopId && x.Status == "Published")).OrderByDescending(x => x.CreatedAt).ToList();
        var valid = orders.Where(x => x.OrderStatus != "Cancelled").ToList();
        var map = items.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => new { Units = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice) });
        var low = products.Where(x => x.Status == "Active" && x.Quantity <= 5).OrderBy(x => x.Quantity).ToList();
        var paidOrderIds = orders
            .Where(order => order.PaymentStatus == "Paid")
            .Select(order => order.OrderId)
            .ToHashSet();
        var promotionItems = items
            .Where(item => item.ProductDealId.HasValue && paidOrderIds.Contains(item.OrderId))
            .ToList();
        var promotionDealIds = promotionItems
            .Select(item => item.ProductDealId!.Value)
            .ToHashSet();
        var promotionDeals = await unit.Repository<ProductDeal>()
            .ListAsync(deal => promotionDealIds.Contains(deal.ProductDealId));
        var promotionAnalytics = new MerchantPromotionAnalyticsDto
        {
            PromotionRevenue = promotionItems.Sum(item => item.TotalPrice),
            UsedQuantity = promotionItems.Sum(item => item.Quantity),
            ActiveDealsCount = promotionDeals.Count(deal => deal.Status == "Active"),
            TotalDiscountAmount = promotionItems.Sum(item =>
            {
                var product = products.FirstOrDefault(product => product.ProductId == item.ProductId);
                return product is null ? 0m : (product.Price - item.UnitPrice) * item.Quantity;
            }),
            Deals = promotionDeals.Select(deal =>
            {
                var dealItems = promotionItems.Where(item => item.ProductDealId == deal.ProductDealId).ToList();
                var product = products.FirstOrDefault(item => item.ProductId == deal.ProductId);
                return new MerchantPromotionDealDto
                {
                    ProductDealId = deal.ProductDealId,
                    ProductName = product?.ProductName ?? "สินค้า",
                    DealType = deal.DealType,
                    UsedQuantity = deal.UsedQuantity,
                    TotalQuantity = deal.TotalQuantity,
                    Revenue = dealItems.Sum(item => item.TotalPrice),
                    DiscountAmount = dealItems.Sum(item => product is null ? 0m : (product.Price - item.UnitPrice) * item.Quantity),
                    Status = deal.Status
                };
            }).OrderByDescending(deal => deal.Revenue).ToList()
        };
        return new MerchantDashboardReportDto { Summary = new MerchantSummaryDto { TotalRevenue = valid.Sum(x => x.TotalAmount), TodayRevenue = valid.Where(x => x.CreatedAt >= today).Sum(x => x.TotalAmount), MonthRevenue = valid.Where(x => x.CreatedAt >= month).Sum(x => x.TotalAmount), TotalOrders = orders.Count, PendingSlipOrdersCount = orders.Count(x => x.PaymentStatus == "Pending" || (!string.IsNullOrEmpty(x.SlipImageUrl) && x.OrderStatus == "Pending")), PendingShipmentOrdersCount = orders.Count(x => x.OrderStatus is "Paid" or "Processing"), CompletedOrdersCount = orders.Count(x => x.OrderStatus == "Completed"), TotalProducts = products.Count, LowStockProductsCount = low.Count, TotalContents = contents.Count }, RecentOrders = orders.Take(5).Select(o => new MerchantRecentOrderDto { OrderId = o.OrderId, OrderNumber = o.OrderNumber, ReceiverName = o.ReceiverName, TotalAmount = o.TotalAmount, OrderStatus = o.OrderStatus, PaymentStatus = o.PaymentStatus, SlipImageUrl = o.SlipImageUrl, SlipUploadedAt = o.SlipUploadedAt, CreatedAt = o.CreatedAt, ItemsCount = items.Count(i => i.OrderId == o.OrderId) }).ToList(), TopProducts = products.Select(p => new MerchantTopProductDto { ProductId = p.ProductId, ProductName = p.ProductName, ImageUrl = p.ImageUrl, Price = p.Price, StockQuantity = p.Quantity, TotalUnitsSold = map.TryGetValue(p.ProductId, out var stat) ? stat.Units : 0, TotalRevenue = map.TryGetValue(p.ProductId, out stat) ? stat.Revenue : 0m }).OrderByDescending(p => p.TotalUnitsSold).ThenByDescending(p => p.TotalRevenue).Take(5).ToList(), SalesTrend = Enumerable.Range(0, 30).Select(offset => { var day = today.AddDays(-29 + offset);
        var dayOrders = valid.Where(x => x.CreatedAt.Date == day).ToList();
        return new MerchantSalesTrendDto { Date = day.ToString("yyyy-MM-dd"), DailyRevenue = dayOrders.Sum(x => x.TotalAmount), OrdersCount = dayOrders.Count }; }).ToList(), LowStockProducts = low.Select(p => new MerchantLowStockProductDto { ProductId = p.ProductId, ProductName = p.ProductName, ImageUrl = p.ImageUrl, Price = p.Price, Quantity = p.Quantity }).ToList(), Fulfillment = new MerchantFulfillmentDto { DeliveryOrdersCount = orders.Count(x => x.ShippingMethod == "Delivery"), PickupOrdersCount = orders.Count(x => x.ShippingMethod == "Pickup"), AverageOrderValue = valid.Count > 0 ? Math.Round(valid.Average(x => x.TotalAmount), 2) : 0m }, PromotionAnalytics = promotionAnalytics, ReviewsSummary = new MerchantReviewsSummaryDto { AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(x => x.Rating), 1), TotalReviews = reviews.Count, RecentReviews = reviews.Take(3).Select(r => new MerchantReviewItemDto { ReviewId = r.ReviewId, UserName = "ลูกค้าสั่งซื้อ", Rating = r.Rating, Comment = r.Comment, Reply = r.Reply, CreatedAt = r.CreatedAt }).ToList() } };
    }

    public async Task<ShopServiceResult> CreateAsync(CreateShopDto dto, string ownerUserId)
    {
        var shop = new Shop { OwnerUserId = ownerUserId };
        CopyDtoToShop(dto, shop);

        try { await business.ValidateCreateAsync(shop); }
        catch (InvalidOperationException ex) { return new(ShopServiceStatus.Conflict, Error: ex.Message); }

        shop.ShopId = Guid.NewGuid();
        shop.Status = ShopStatuses.PendingApproval;
        shop.CreatedAt = shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Add(shop);

        return await unit.Complete()
            ? new(ShopServiceStatus.Success, shop)
            : new(ShopServiceStatus.Failed, Error: "Problem creating shop");
    }

    public async Task<ShopServiceResult> UpdateAsync(Guid shopId, UpdateShopDto dto, string userId, bool isAdmin)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(shopId);
        if (shop == null) return new(ShopServiceStatus.NotFound);
        if (!IsOwnerOrAdmin(shop, userId, isAdmin)) return new(ShopServiceStatus.Forbidden);
        if (!isAdmin && shop.Status is ShopStatuses.Closed or ShopStatuses.Suspended)
            return new(ShopServiceStatus.Invalid, Error: "This shop cannot be updated in its current status");

        CopyDtoToShop(dto, shop);
        shop.UpdatedAt = DateTime.UtcNow;
        if (!isAdmin && shop.Status == ShopStatuses.Rejected) shop.Status = ShopStatuses.PendingApproval;

        try { await business.ValidateUpdateAsync(shop); }
        catch (InvalidOperationException ex) { return new(ShopServiceStatus.Invalid, Error: ex.Message); }

        return await unit.Complete()
            ? new(ShopServiceStatus.Success, shop)
            : new(ShopServiceStatus.Failed, Error: "Problem updating shop");
    }

    public async Task<ShopServiceResult> UploadImageAsync(Guid shopId, IFormFile file, string userId, bool isAdmin, bool isBackground)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(shopId);
        if (shop == null) return new(ShopServiceStatus.NotFound);
        if (!IsOwnerOrAdmin(shop, userId, isAdmin)) return new(ShopServiceStatus.Forbidden);

        var savedImage = await imageUploadService.SaveAsync(file, "shops");
        if (savedImage == null) return new(ShopServiceStatus.Invalid, Error: "Only JPG, PNG, WEBP files up to 5 MB are allowed.");

        if (isBackground) shop.BackgroundImageUrl = savedImage.Url;
        else shop.CoverImageUrl = savedImage.Url;
        shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Update(shop);

        if (await unit.Complete()) return new(ShopServiceStatus.Success, shop);

        imageUploadService.Delete(savedImage);
        return new(ShopServiceStatus.Failed, Error: "Problem uploading shop image");
    }

    public async Task<ShopServiceResult> CloseAsync(Guid shopId, string userId, bool isAdmin)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(shopId);
        if (shop == null) return new(ShopServiceStatus.NotFound);
        if (!IsOwnerOrAdmin(shop, userId, isAdmin)) return new(ShopServiceStatus.Forbidden);

        shop.Status = ShopStatuses.Closed;
        shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Update(shop);
        return await unit.Complete()
            ? new(ShopServiceStatus.Success, shop)
            : new(ShopServiceStatus.Failed, Error: "Problem closing shop");
    }

    public async Task<ShopServiceResult> UpdateStatusAsync(Guid shopId, string status, string userId, bool isAdmin)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(shopId);
        if (shop == null) return new(ShopServiceStatus.NotFound);
        if (!IsOwnerOrAdmin(shop, userId, isAdmin)) return new(ShopServiceStatus.Forbidden);

        var allowed = isAdmin
            ? new[] { ShopStatuses.PendingApproval, ShopStatuses.Active, ShopStatuses.Rejected, ShopStatuses.Inactive, ShopStatuses.Closed, ShopStatuses.Suspended }
            : new[] { ShopStatuses.Active, ShopStatuses.Closed };
        if (!isAdmin && !((shop.Status == ShopStatuses.Active && status == ShopStatuses.Closed) || (shop.Status == ShopStatuses.Closed && status == ShopStatuses.Active)))
            return new(ShopServiceStatus.Forbidden);
        var normalizedStatus = allowed.FirstOrDefault(x => x.Equals(status, StringComparison.OrdinalIgnoreCase));
        if (normalizedStatus == null) return new(ShopServiceStatus.Invalid, Error: "Invalid shop status");

        shop.Status = normalizedStatus;
        shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Update(shop);
        return await unit.Complete()
            ? new(ShopServiceStatus.Success, shop)
            : new(ShopServiceStatus.Failed, Error: "Problem updating shop status");
    }

    private static bool IsOwnerOrAdmin(Shop shop, string userId, bool isAdmin)
    {
        return isAdmin || shop.OwnerUserId == userId;
    }

    private static ShopDto ToDto(Shop shop, bool includeOwner = false)
    {
        return new ShopDto
        {
            ShopId = shop.ShopId,
            OwnerUserId = includeOwner ? shop.OwnerUserId : null,
            ShopCategoryId = shop.ShopCategoryId,
            DistrictId = shop.DistrictId,
            SubDistrictId = shop.SubDistrictId,
            ShopName = shop.ShopName,
            Description = shop.Description,
            Phone = shop.Phone,
            Email = shop.Email,
            Address = shop.Address,
            CoverImageUrl = shop.CoverImageUrl,
            BackgroundImageUrl = shop.BackgroundImageUrl,
            OpeningTime = shop.OpeningTime,
            ClosingTime = shop.ClosingTime,
            Latitude = shop.Latitude,
            Longitude = shop.Longitude,
            Status = shop.Status,
            BankName = shop.BankName,
            BankAccountName = shop.BankAccountName,
            BankAccountNumber = shop.BankAccountNumber,
            PromptPay = shop.PromptPay,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.UpdatedAt,
            CategoryName = shop.ShopCategory?.CategoryName,
            DistrictName = shop.District?.DistrictName,
            SubDistrictName = shop.SubDistrict?.SubDistrictName
        };
    }

    private static void CopyDtoToShop(CreateShopDto dto, Shop shop)
    {
        shop.ShopName = dto.ShopName;
        shop.ShopCategoryId = dto.ShopCategoryId;
        shop.DistrictId = dto.DistrictId;
        shop.SubDistrictId = dto.SubDistrictId;
        shop.Description = dto.Description;
        shop.Phone = dto.Phone;
        shop.Email = dto.Email;
        shop.Address = dto.Address;
        shop.OpeningTime = dto.OpeningTime;
        shop.ClosingTime = dto.ClosingTime;
        shop.Latitude = dto.Latitude;
        shop.Longitude = dto.Longitude;
        shop.BankName = dto.BankName;
        shop.BankAccountName = dto.BankAccountName;
        shop.BankAccountNumber = dto.BankAccountNumber;
        shop.PromptPay = dto.PromptPay;
    }

}
