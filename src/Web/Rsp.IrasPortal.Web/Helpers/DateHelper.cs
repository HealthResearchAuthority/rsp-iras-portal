using System.Globalization;

namespace Rsp.IrasPortal.Web.Helpers;

public static class DateHelper
{
    public static string GetFormattedDateWithOrdinal(DateTime? date)
    {
        if (date == null) return string.Empty;

        var actualDate = date.Value;
        var day = actualDate.Day.ToString();
        var suffix = GetDaySuffix(actualDate.Day);
        var formattedDate = $"{day}{suffix} {actualDate.ToString("MMMM yyyy HH:mm", new CultureInfo("en-GB"))}";

        return formattedDate;
    }

    private static string GetDaySuffix(int day)
    {
        if (day is >= 11 and <= 13) return "th";
        return (day % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}