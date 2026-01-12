using System.Diagnostics.CodeAnalysis;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Models;

[ExcludeFromCodeCoverage]
public class ApplicationSearchModel
{
    public string? SearchTitleTerm { get; set; }

    public DateTime? FromDate => DateTimeExtensions.ParseDateValidation(FromDay, FromMonth, FromYear);
    public DateTime? ToDate => DateTimeExtensions.ParseDateValidation(ToDay, ToMonth, ToYear);

    public string? FromDay { get; set; }
    public string? FromMonth { get; set; }
    public string? FromYear { get; set; }

    public string? ToDay { get; set; }
    public string? ToMonth { get; set; }
    public string? ToYear { get; set; }

    public List<string> Status { get; set; } = [];

    public Dictionary<string, List<string>>? Filters
    {
        get
        {
            if (IgnoreFilters)
            {
                IgnoreFilters = false;
                return null;
            }

            var filters = new Dictionary<string, List<string>>();

            if (FromDate.HasValue && ToDate.HasValue)
            {
                // Both dates entered — show combined range only
                filters.Add(ApplicationSearch.DateRangeKey,
                    [$"{FromDate.Value:d MMM yyyy} to {ToDate.Value:d MMM yyyy}"]);
            }
            else
            {
                // Only one date entered — show individual filter
                if (FromDate.HasValue)
                {
                    filters.Add(ApplicationSearch.FromDateKey, [FromDate.Value.ToString("d MMM yyyy")]);
                }

                if (ToDate.HasValue)
                {
                    filters.Add(ApplicationSearch.ToDateKey, [ToDate.Value.ToString("d MMM yyyy")]);
                }
            }

            if (Status.Count != 0)
            {
                filters.Add(ApplicationSearch.StatusKey, Status);
            }

            return filters;
        }
    }

    public bool IgnoreFilters { get; set; }
}