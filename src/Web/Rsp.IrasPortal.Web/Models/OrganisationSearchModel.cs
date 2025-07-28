using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class OrganisationSearchModel
{
    public string? SearchNameTerm { get; set; }
    public List<string> Country { get; set; } = [];
    public List<string> OrganisationTypes { get; set; } = [];

    public Dictionary<string, List<string>> Filters
    {
        get
        {
            var filters = new Dictionary<string, List<string>>();

            if (Country.Count != 0)
            {
                filters.Add(OrganisationSearch.CountryKey, Country);
            }

            if (OrganisationTypes.Count != 0)
            {
                filters.Add(OrganisationSearch.OrganisationTypeKey, OrganisationTypes);
            }

            return filters;
        }
    }
}