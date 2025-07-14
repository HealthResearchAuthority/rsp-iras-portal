using System.Diagnostics.CodeAnalysis;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class ReviewBodySearchModel
{
    public string? SearchQuery { get; set; }
    public List<string> Country { get; set; } = [];
    public bool? Status { get; set; }

    public Dictionary<string, List<string>> Filters
    {
        get
        {
            var filters = new Dictionary<string, List<string>>();

            if (Country?.Count > 0)
            {
                filters.Add("Country", Country);
            }

            if (Status != null)
            {
                filters.Add("Status", [Status == true ? "Active" : "Disabled"]);
            }

            return filters;
        }
    }

}