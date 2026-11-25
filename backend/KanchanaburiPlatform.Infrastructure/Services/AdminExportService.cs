using Application.Results;
using Core.Interfaces;
using KanchanaburiPlatform.Application.Common.Export;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AdminExportService(
    StoreContext context
) : IAdminExportService
{
    public async Task<ServiceResult<ExportFileResult>> ExportOrdersAsync(
        DateTime? from,
        DateTime? to,
        Guid? shopId,
        string? orderStatus,
        string? paymentStatus,
        string? payoutStatus,
        bool pdf
    )
    {
        if (!DateRangeHelper.TryRange(
            from,
            to,
            out var start,
            out var end,
            out var error
        ))
        {
            return ExportHelper.Invalid(error!);
        }

        var rows = await context.Orders
            .AsNoTracking()
            .Where(o =>
                o.CreatedAt >= start &&
                o.CreatedAt < end &&
                (!shopId.HasValue || o.ShopId == shopId) &&
                (string.IsNullOrWhiteSpace(orderStatus) ||
                 o.OrderStatus == orderStatus) &&
                (string.IsNullOrWhiteSpace(paymentStatus) ||
                 o.PaymentStatus == paymentStatus) &&
                (string.IsNullOrWhiteSpace(payoutStatus) ||
                 o.PayoutStatus == payoutStatus)
            )
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.OrderNumber,
                o.CreatedAt,
                ShopName = o.Shop.ShopName,
                o.ReceiverName,
                o.ReceiverPhone,
                o.Subtotal,
                o.ShippingFee,
                o.TotalAmount,
                o.OrderStatus,
                o.PaymentStatus,
                o.PayoutStatus,
                Shipment = o.Shipments
                    .OrderByDescending(s => s.ShippedAt)
                    .Select(s => new
                    {
                        s.ShippingProvider,
                        s.TrackingNumber
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var csv = new CsvBuilder();

        csv.Row(
            "เลขออเดอร์",
            "วันที่สั่งซื้อ",
            "ร้านค้า",
            "ลูกค้า",
            "เบอร์โทร",
            "ยอดสินค้า",
            "ค่าจัดส่ง",
            "ยอดรวม",
            "สถานะออเดอร์",
            "สถานะชำระเงิน",
            "สถานะการโอนให้ร้าน",
            "บริษัทขนส่ง",
            "เลขพัสดุ"
        );

        foreach (var row in rows)
        {
            csv.Row(
                row.OrderNumber,
                FormatHelper.Date(row.CreatedAt),
                row.ShopName,
                row.ReceiverName,
                row.ReceiverPhone,
                FormatHelper.Money(row.Subtotal),
                FormatHelper.Money(row.ShippingFee),
                FormatHelper.Money(row.TotalAmount),
                StatusText.Order(row.OrderStatus),
                StatusText.Payment(row.PaymentStatus),
                StatusText.Payout(row.PayoutStatus),
                row.Shipment?.ShippingProvider,
                row.Shipment?.TrackingNumber
            );
        }

        return ExportHelper.Success(
            csv,
            $"admin-orders-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}",
            pdf,
            rows => ExportPdf.Create(
                "รายงานออเดอร์ทั้งหมด",
                rows
            )
        );
    }

    public async Task<ServiceResult<ExportFileResult>> ExportShopSalesAsync(
        DateTime? from,
        DateTime? to,
        bool pdf
    )
    {
        if (!DateRangeHelper.TryRange(
            from,
            to,
            out var start,
            out var end,
            out var error
        ))
        {
            return ExportHelper.Invalid(error!);
        }

        var rows = await context.Orders
            .AsNoTracking()
            .Where(o =>
                o.CreatedAt >= start &&
                o.CreatedAt < end
            )
            .GroupBy(o => new
            {
                o.ShopId,
                o.Shop.ShopName
            })
            .Select(g => new
            {
                g.Key.ShopId,
                g.Key.ShopName,

                PaidOrders = g.Count(
                    o => o.PaymentStatus == "Paid"
                ),

                PaidSales = g
                    .Where(o => o.PaymentStatus == "Paid")
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m,

                SettledAmount = g
                    .Where(o =>
                        o.PaymentStatus == "Paid" &&
                        o.PayoutStatus == "Settled"
                    )
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m,

                PendingPayoutAmount = g
                    .Where(o =>
                        o.PaymentStatus == "Paid" &&
                        o.PayoutStatus != "Settled"
                    )
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m
            })
            .OrderByDescending(x => x.PaidSales)
            .ToListAsync();

        var csv = new CsvBuilder();

        csv.Row(
            "รหัสร้าน",
            "ชื่อร้าน",
            "จำนวนออเดอร์ชำระแล้ว",
            "ยอดขายรวม",
            "ยอดโอนแล้ว",
            "ยอดค้างโอน"
        );

        foreach (var row in rows)
        {
            csv.Row(
                row.ShopId,
                row.ShopName,
                row.PaidOrders,
                FormatHelper.Money(row.PaidSales),
                FormatHelper.Money(row.SettledAmount),
                FormatHelper.Money(row.PendingPayoutAmount)
            );
        }

        return ExportHelper.Success(
            csv,
            $"admin-shop-sales-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}",
            pdf,
            rows => ExportPdf.Create(
                "รายงานยอดขายแยกร้าน",
                rows
            )
        );
    }

    public async Task<ServiceResult<ExportFileResult>> ExportPayoutsAsync(
        DateTime? from,
        DateTime? to,
        Guid? shopId,
        bool pdf
    )
    {
        if (!DateRangeHelper.TryRange(
            from,
            to,
            out var start,
            out var end,
            out var error
        ))
        {
            return ExportHelper.Invalid(error!);
        }

        var rows = await context.MerchantPayouts
            .AsNoTracking()
            .Where(p =>
                p.CreatedAt >= start &&
                p.CreatedAt < end &&
                (!shopId.HasValue || p.ShopId == shopId)
            )
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.PayoutId,
                p.CreatedAt,
                ShopName = p.Shop.ShopName,
                p.TotalAmount,
                p.Status,
                p.TransactionRef,
                p.Note,
                p.SlipImageUrl,
                OrdersCount = p.Orders.Count
            })
            .ToListAsync();

        var csv = new CsvBuilder();

        csv.Row(
            "รหัส payout",
            "วันที่โอน",
            "ร้านค้า",
            "ยอดเงิน",
            "สถานะการโอนให้ร้าน",
            "เลขอ้างอิง",
            "หมายเหตุ",
            "หลักฐานการโอน",
            "จำนวนออเดอร์"
        );

        foreach (var row in rows)
        {
            csv.Row(
                row.PayoutId,
                FormatHelper.Date(row.CreatedAt),
                row.ShopName,
                FormatHelper.Money(row.TotalAmount),
                StatusText.Payout(row.Status),
                row.TransactionRef,
                row.Note,
                row.SlipImageUrl,
                row.OrdersCount
            );
        }

        return ExportHelper.Success(
            csv,
            $"admin-payouts-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}",
            pdf,
            rows => ExportPdf.Create(
                "รายงานการโอนเงินให้ร้านค้า",
                rows
            )
        );
    }

    public async Task<ServiceResult<ExportFileResult>> ExportPaymentSlipsAsync(
        DateTime? from,
        DateTime? to,
        string? paymentStatus,
        bool pdf
    )
    {
        if (!DateRangeHelper.TryRange(
            from,
            to,
            out var start,
            out var end,
            out var error
        ))
        {
            return ExportHelper.Invalid(error!);
        }

        var rows = await context.Orders
            .AsNoTracking()
            .Where(o =>
                o.CreatedAt >= start &&
                o.CreatedAt < end &&
                !string.IsNullOrEmpty(o.SlipImageUrl) &&
                (
                    string.IsNullOrWhiteSpace(paymentStatus) ||
                    o.PaymentStatus == paymentStatus
                )
            )
            .OrderByDescending(
                o => o.SlipUploadedAt ?? o.CreatedAt
            )
            .Select(o => new
            {
                o.OrderNumber,
                ShopName = o.Shop.ShopName,
                o.ReceiverName,
                o.TotalAmount,
                o.PaymentStatus,
                o.SlipImageUrl,
                o.SlipUploadedAt
            })
            .ToListAsync();

        var csv = new CsvBuilder();

        csv.Row(
            "เลขออเดอร์",
            "ร้านค้า",
            "ลูกค้า",
            "ยอดที่ชำระ",
            "สถานะชำระเงิน",
            "วันที่อัปโหลดสลิป",
            "URL สลิป"
        );

        foreach (var row in rows)
        {
            csv.Row(
                row.OrderNumber,
                row.ShopName,
                row.ReceiverName,
                FormatHelper.Money(row.TotalAmount),
                StatusText.Payment(row.PaymentStatus),
                FormatHelper.Date(row.SlipUploadedAt),
                row.SlipImageUrl
            );
        }

        return ExportHelper.Success(
            csv,
            $"admin-payment-slips-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}",
            pdf,
            rows => ExportPdf.Create(
                "รายงานสลิปชำระเงิน",
                rows
            )
        );
    }
}
