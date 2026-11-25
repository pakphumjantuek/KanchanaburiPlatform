namespace KanchanaburiPlatform.Application.Common.Export;

public static class DateRangeHelper
{
    public static bool TryRange(
        DateTime? from,
        DateTime? to,
        out DateTime start,
        out DateTime end,
        out string? error
    )
    {
        var now = DateTime.UtcNow;

        start = (from ?? new DateTime(
            now.Year,
            now.Month,
            1
        )).Date;

        end = (to ?? now)
            .Date
            .AddDays(1);

        error = null;

        // วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มต้น
        if (end <= start)
        {
            error = "'to' must be on or after 'from'.";
            return false;
        }

        // จำกัดช่วงข้อมูลไม่เกิน 366 วัน
        if (end - start > TimeSpan.FromDays(366))
        {
            error = "Date range cannot exceed 366 days.";
            return false;
        }

        return true;
    }
}