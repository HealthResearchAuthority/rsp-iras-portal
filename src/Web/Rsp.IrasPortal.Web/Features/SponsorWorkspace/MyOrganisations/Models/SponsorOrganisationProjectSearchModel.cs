using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorOrganisationProjectSearchModel
{
    public string? IrasId { get; set; }

    public DateTime? FromDate => DateTimeExtensions.ParseDateValidation(FromDay, FromMonth, FromYear);
    public DateTime? ToDate => DateTimeExtensions.ParseDateValidation(ToDay, ToMonth, ToYear);

    public string? FromDay { get; set; }
    public string? FromMonth { get; set; }
    public string? FromYear { get; set; }

    public string? ToDay { get; set; }
    public string? ToMonth { get; set; }
    public string? ToYear { get; set; }
    public string? Status { get; set; }

    public Dictionary<string, List<string>>? Filters =>
    IgnoreFilters ? null : BuildFilters();

    public bool IgnoreFilters { get; set; }

    private Dictionary<string, List<string>> BuildFilters()
    {
        var filters = new Dictionary<string, List<string>>();

        if (FromDate.HasValue && ToDate.HasValue)
        {
            filters.Add(
                "Created date",
                [$"{FromDate.Value:d MMM yyyy} to {ToDate.Value:d MMM yyyy}"]
            );
        }
        else
        {
            if (FromDate.HasValue)
            {
                filters.Add(
                    "Date created - from",
                    [FromDate.Value.ToString("d MMM yyyy")]
                );
            }

            if (ToDate.HasValue)
            {
                filters.Add(
                    "Date created - to",
                    [ToDate.Value.ToString("d MMM yyyy")]
                );
            }
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            filters.Add(ApprovalsSearch.StatusKey, [Status]);
        }

        return filters;
    }
}