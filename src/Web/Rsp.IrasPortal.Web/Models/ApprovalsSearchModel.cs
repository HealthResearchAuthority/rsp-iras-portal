using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class ApprovalsSearchModel
{
    public string? IrasId { get; set; }
    public string? ChiefInvestigatorName { get; set; }
    public string? ShortProjectTitle { get; set; }
    public string? SponsorOrganisation { get; set; }

    public DateTime? FromDate => ParseDate(FromDay, FromMonth, FromYear);
    public DateTime? ToDate => ParseDate(ToDay, ToMonth, ToYear);

    public string? FromDay { get; set; }
    public string? FromMonth { get; set; }
    public string? FromYear { get; set; }

    public string? ToDay { get; set; }
    public string? ToMonth { get; set; }
    public string? ToYear { get; set; }

    public List<string> Country { get; set; } = [];
    public List<string> ModificationTypes { get; set; } = [];
    public OrganisationSearchViewModel SponsorOrgSearch { get; set; } = new();

    public Dictionary<string, List<string>> Filters
    {
        get
        {
            var filters = new Dictionary<string, List<string>>();

            if (!string.IsNullOrWhiteSpace(ChiefInvestigatorName))
            {
                filters.Add(ApprovalsSearch.ChiefInvestigatorKey, [ChiefInvestigatorName]);
            }

            if (!string.IsNullOrWhiteSpace(ShortProjectTitle))
            {
                filters.Add(ApprovalsSearch.ShortProjectTitleKey, [ShortProjectTitle]);
            }

            if (!string.IsNullOrWhiteSpace(SponsorOrgSearch.SelectedOrganisation))
            {
                SponsorOrganisation = SponsorOrgSearch.SelectedOrganisation;
            }

            if (!string.IsNullOrWhiteSpace(SponsorOrganisation))
            {
                filters.Add(ApprovalsSearch.SponsorOrganisationKey, [SponsorOrganisation]);
            }

            if (FromDate.HasValue)
            {
                filters.Add(ApprovalsSearch.FromDateKey, [FromDate.Value.ToString("d MMM yyyy")]);
            }

            if (ToDate.HasValue)
            {
                filters.Add(ApprovalsSearch.ToDateKey, [ToDate.Value.ToString("d MMM yyyy")]);
            }

            if (Country.Count != 0)
            {
                filters.Add(ApprovalsSearch.LeadNationKey, Country);
            }

            if (ModificationTypes.Count != 0)
            {
                filters.Add(ApprovalsSearch.ModificationTypeKey, ModificationTypes);
            }

            return filters;
        }
    }

    private static DateTime? ParseDate(string? day, string? month, string? year)
    {
        if (string.IsNullOrWhiteSpace(day) || string.IsNullOrWhiteSpace(month) || string.IsNullOrWhiteSpace(year))
            return null;

        // Ensure year is 4 digits
        if (!Regex.IsMatch(year, @"^\d{4}$"))
            return null;

        if (!int.TryParse(day, out var d) ||
            !int.TryParse(month, out var m) ||
            !int.TryParse(year, out var y))
            return null;

        // Optionally enforce a year range (e.g., 1900–2100)
        if (y < 1900 || y > 2100)
            return null;

        return DateTime.TryParseExact(
            $"{y:D4}-{m:D2}-{d:D2}",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var result)
            ? result
            : null;
    }
}