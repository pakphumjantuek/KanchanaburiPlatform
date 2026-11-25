using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Application.Common.Export;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class MerchantExportService(StoreContext context) : IMerchantExportService
{
    public async Task<ServiceResult<ExportFileResult>> ExportOrdersAsync(string userId, DateTime? from, DateTime? to, string? orderStatus, bool pdf)
    {
        if (!DateRangeHelper.TryRange(from, to, out var start, out var end, out var error))
        {
            return ExportHelper.Invalid(error!);
        }

        var shop = await FindShopAsync(userId);
        if (shop is null)
        {
            return NotFound();
        }

        var rows = await context.Orders.AsNoTracking()
            .Where(o => o.ShopId == shop.ShopId && o.CreatedAt >= start && o.CreatedAt < end &&
                (string.IsNullOrWhiteSpace(orderStatus) || o.OrderStatus == orderStatus))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.OrderNumber, o.CreatedAt, o.ReceiverName, o.ReceiverPhone,
                o.Subtotal, o.ShippingFee, o.TotalAmount, o.OrderStatus,
                Shipment = o.Shipments.OrderByDescending(s => s.ShippedAt)
                    .Select(s => new { s.ShippingProvider, s.TrackingNumber, s.ShippedAt, s.DeliveredAt })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var csv = new CsvBuilder();
        csv.Row("เลขออเดอร์", "วันที่สั่งซื้อ", "ชื่อลูกค้า", "เบอร์โทร", "ยอดสินค้า", "ค่าจัดส่ง", "ยอดรวม", "สถานะออเดอร์", "บริษัทขนส่ง", "เลขพัสดุ", "วันที่จัดส่ง", "วันที่สำเร็จ");
        foreach (var row in rows)
        {
            csv.Row(row.OrderNumber, FormatHelper.Date(row.CreatedAt), row.ReceiverName, row.ReceiverPhone, FormatHelper.Money(row.Subtotal), FormatHelper.Money(row.ShippingFee), FormatHelper.Money(row.TotalAmount), StatusText.Order(row.OrderStatus), row.Shipment?.ShippingProvider, row.Shipment?.TrackingNumber, FormatHelper.Date(row.Shipment?.ShippedAt), FormatHelper.Date(row.Shipment?.DeliveredAt));
        }

        return ExportHelper.Success(csv, $"orders-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}", pdf, rows => ExportPdf.Create("รายงานออเดอร์ร้านค้า", rows));
    }

    public async Task<ServiceResult<ExportFileResult>> ExportProductsAsync(string userId, DateTime? from, DateTime? to, bool pdf)
    {
        if (!DateRangeHelper.TryRange(from, to, out var start, out var end, out var error))
        {
            return ExportHelper.Invalid(error!);
        }

        var shop = await FindShopAsync(userId);
        if (shop is null)
        {
            return NotFound();
        }

        var rows = await context.OrderItems.AsNoTracking()
            .Where(i => i.Order.ShopId == shop.ShopId && i.Order.CreatedAt >= start && i.Order.CreatedAt < end && i.Order.PaymentStatus == "Paid")
            .GroupBy(i => new { i.ProductId, i.ProductName, i.Product.Price, i.Product.Quantity, i.Product.Status })
            .Select(g => new { g.Key.ProductId, g.Key.ProductName, g.Key.Price, g.Key.Quantity, g.Key.Status, UnitsSold = g.Sum(i => i.Quantity), TotalSales = g.Sum(i => i.TotalPrice), OrdersCount = g.Select(i => i.OrderId).Distinct().Count() })
            .OrderByDescending(x => x.TotalSales)
            .ToListAsync();

        var csv = new CsvBuilder();
        csv.Row("รหัสสินค้า", "ชื่อสินค้า", "จำนวนที่ขาย", "ยอดขายรวม", "ราคาเฉลี่ยต่อชิ้น", "จำนวนออเดอร์", "สต็อกคงเหลือ", "สถานะสินค้า");
        foreach (var row in rows)
        {
            var averagePrice = row.UnitsSold == 0 ? 0m : row.TotalSales / row.UnitsSold;
            csv.Row(row.ProductId, row.ProductName, row.UnitsSold, FormatHelper.Money(row.TotalSales), FormatHelper.Money(averagePrice), row.OrdersCount, row.Quantity, row.Status);
        }

        return ExportHelper.Success(csv, $"products-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}", pdf, rows => ExportPdf.Create("รายงานยอดขายสินค้า", rows));
    }

    public async Task<ServiceResult<ExportFileResult>> ExportPayoutsAsync(string userId, DateTime? from, DateTime? to, bool pdf)
    {
        if (!DateRangeHelper.TryRange(from, to, out var start, out var end, out var error))
        {
            return ExportHelper.Invalid(error!);
        }

        var shop = await FindShopAsync(userId);
        if (shop is null)
        {
            return NotFound();
        }

        var rows = await context.MerchantPayouts.AsNoTracking()
            .Where(p => p.ShopId == shop.ShopId && p.CreatedAt >= start && p.CreatedAt < end)
            .OrderByDescending(p => p.CreatedAt)
            .SelectMany(p => p.Orders.Select(o => new { p.PayoutId, p.CreatedAt, PayoutTotalAmount = p.TotalAmount, p.Status, p.TransactionRef, p.Note, p.SlipImageUrl, o.OrderNumber, OrderTotalAmount = o.TotalAmount }))
            .ToListAsync();

        var csv = new CsvBuilder();
        csv.Row("รหัสรายการโอน", "วันที่โอน", "ยอดเงินที่ได้รับ", "สถานะ", "เลขอ้างอิง", "หมายเหตุ", "หลักฐานการโอน", "เลขออเดอร์", "ยอดออเดอร์");
        foreach (var row in rows)
        {
            csv.Row(row.PayoutId, FormatHelper.Date(row.CreatedAt), FormatHelper.Money(row.PayoutTotalAmount), StatusText.Payout(row.Status), row.TransactionRef, row.Note, row.SlipImageUrl, row.OrderNumber, FormatHelper.Money(row.OrderTotalAmount));
        }

        return ExportHelper.Success(csv, $"payouts-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}", pdf, rows => ExportPdf.Create("รายงานประวัติการรับเงิน", rows));
    }

    private async Task<Shop?> FindShopAsync(string userId)
    {
        return await context.Shops.AsNoTracking()
            .FirstOrDefaultAsync(shop => shop.OwnerUserId == userId);
    }

    private static ServiceResult<ExportFileResult> NotFound()
    {
        return ServiceResult<ExportFileResult>.Failure(
            ServiceStatus.NotFound,
            "Shop not found");
    }

}
