using System.Diagnostics.CodeAnalysis;
using Rsp.Portal.Application.Constants;

namespace Rsp.Portal.Web.Models;

[ExcludeFromCodeCoverage]
public class OrganisationSearchModel
{
    public string? SearchNameTerm { get; set; }
    public List<string> ExcludingRoles { get; set; } = [OrganisationRoles.Sponsor];
    public List<string> Countries { get; set; } = [];
    public List<string> OrganisationTypes { get; set; } = [];
    public List<string> OrganisationStatuses { get; set; } = [];

    public Dictionary<string, List<string>> Filters
    {
        get
        {
            var filters = new Dictionary<string, List<string>>();

            if (Countries.Count != 0)
            {
                filters.Add(OrganisationSearch.CountryKey, Countries);
            }

            if (OrganisationTypes.Count != 0)
            {
                filters.Add(OrganisationSearch.OrganisationTypeKey, OrganisationTypes);
            }

            if (OrganisationStatuses.Count != 0)
            {
                filters.Add(OrganisationSearch.OrganisationStatusKey, OrganisationStatuses);
            }

            return filters;
        }
    }
}