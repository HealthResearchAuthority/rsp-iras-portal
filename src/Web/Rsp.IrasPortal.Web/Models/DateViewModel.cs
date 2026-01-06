using System.Globalization;

namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel for representing a date using separate day, month, and year string properties.
/// </summary>
public class DateViewModel
{
    /// <summary>
    /// Gets the parsed <see cref="DateTime"/> value from the Day, Month, and Year properties, or null if invalid.
    /// </summary>
    public DateTime? Date => ParseDate(Day, Month, Year);

    /// <summary>
    /// Gets or sets the day component as a string.
    /// </summary>
    public string? Day { get; set; }

    /// <summary>
    /// Gets or sets the month component as a string.
    /// </summary>
    public string? Month { get; set; }

    /// <summary>
    /// Gets or sets the year component as a string.
    /// </summary>
    public string? Year { get; set; }

    //public static DateViewModel FromDate(DateTime? date)
    //       => date is null
    //          ? new DateViewModel()
    //          : new DateViewModel
    //          {
    //              Day = date.Value.Day.ToString(CultureInfo.InvariantCulture),
    //              Month = date.Value.Month.ToString(CultureInfo.InvariantCulture),
    //              Year = date.Value.Year.ToString(CultureInfo.InvariantCulture)
    //          };

    /// <summary>
    /// Attempts to parse the provided day, month, and year strings into a <see cref="DateTime"/> object.
    /// Returns null if parsing fails.
    /// </summary>
    /// <param name="day">The day component as a string.</param>
    /// <param name="month">The month component as a string.</param>
    /// <param name="year">The year component as a string.</param>
    /// <returns>A <see cref="DateTime"/> if parsing succeeds; otherwise, null.</returns>
    private static DateTime? ParseDate(string? day, string? month, string? year)
    {
        return int.TryParse(day, out var d) &&
               int.TryParse(month, out var m) &&
               int.TryParse(year, out var y) &&
               DateTime.TryParse(
                   $"{y:D4}-{m:D2}-{d:D2}",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out var result
               ) ? result : null;
    }
}