using System.Globalization;

namespace Rsp.IrasPortal.Web.Helpers;

public static class DateHelper
{
    public static string ConvertDateToString(string inputDate)
    {
        if (!string.IsNullOrEmpty(inputDate))
        {
            var ukCulture = new CultureInfo("en-GB");
            if (DateTime.TryParse(inputDate, ukCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate.ToString("dd MMMM yyyy");
            }
        }
        return string.Empty;
    }

    public static string ConvertDateToString(DateTime inputDate)
    {
        return inputDate.ToString("dd MMMM yyyy", new CultureInfo("en-GB"));
    }
}