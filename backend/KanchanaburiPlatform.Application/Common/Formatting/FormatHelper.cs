using System.Globalization;

namespace KanchanaburiPlatform.Application.Common.Export;

public static class FormatHelper
{
    public static string Money(decimal value)
    {
        return value.ToString(
            "0.00",
            CultureInfo.InvariantCulture
        );
    }

    public static string Date(DateTime? value)
    {
        return value?.ToString(
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        ) ?? string.Empty;
    }
}