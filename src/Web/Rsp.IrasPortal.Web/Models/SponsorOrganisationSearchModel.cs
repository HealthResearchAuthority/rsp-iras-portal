using System.Diagnostics.CodeAnalysis;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class SponsorOrganisationSearchModel
{
    public string? SearchQuery { get; set; }
    public List<string> Country { get; set; } = [];
    public bool? Status { get; set; }

    public Dictionary<string, string> Filters
    {
        get
        {
            var filters = new Dictionary<string, string>();

            if (Country?.Count != 0 && Country != null)
            {
                filters.Add("Country", string.Join(", ", Country));
            }

            switch (Status)
            {
                case true:
                    filters.Add("Status", "Active");
                    break;
                case false:
                    filters.Add("Status", "Disabled");
                    break;
            }

            return filters;
        }
    }
}