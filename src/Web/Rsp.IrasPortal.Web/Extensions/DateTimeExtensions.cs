using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Rsp.Portal.Web.Extensions;

[ExcludeFromCodeCoverage]
public static  class DateTimeExtensions
{
    public static DateTime? ParseDateValidation(string? day, string? month, string? year)
    {
        if (string.IsNullOrWhiteSpace(day) || string.IsNullOrWhiteSpace(month) || string.IsNullOrWhiteSpace(year))
            return null;

        if (!int.TryParse(day, out var d) ||
            !int.TryParse(month, out var m) ||
            !int.TryParse(year, out var y))
            return null;

        // Optionally enforce a year range (e.g., 1900–2100)
        if (y < 1900 || y > 2100)
            return null;

        return DateTime.TryParseExact(
            $"{y:D4}-{m:D2}-{d:D2}",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var result)
            ? result
            : null;
    }
}
