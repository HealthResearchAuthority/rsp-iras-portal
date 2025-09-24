using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class ProjectDocumentsSearchModel
{
    public string? SearchQuery { get; set; }
    public string? IrasId { get; set; }
    public DateTime? FromDate => DateTimeExtensions.ParseDateValidation(FromDay, FromMonth, FromYear);
    public DateTime? ToDate => DateTimeExtensions.ParseDateValidation(ToDay, ToMonth, ToYear);

    public string? FromDay { get; set; }
    public string? FromMonth { get; set; }
    public string? FromYear { get; set; }

    public string? ToDay { get; set; }
    public string? ToMonth { get; set; }
    public string? ToYear { get; set; }

    public List<string> DocumentTypes { get; set; } = [];

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

            if (DocumentTypes.Count != 0)
            {
                filters.Add("ApprovalsSearch.ModificationTypeKey", DocumentTypes);
            }

            return filters;
        }
    }

    public bool IgnoreFilters { get; set; }
}