using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

    public int? FromSubmission => int.TryParse(FromDaysSinceSubmission, out var days) ? days : null;

    public int? ToSubmission => int.TryParse(ToDaysSinceSubmission, out var days) ? days : null;

    public string? FromDaysSinceSubmission { get; set; }
    public string? ToDaysSinceSubmission { get; set; }

    public List<string> LeadNation { get; set; } = [];
    public List<string> ParticipatingNation { get; set; } = [];
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

            if (FromDate.HasValue && ToDate.HasValue)
            {
                // Both dates entered — show combined range only
                filters.Add(ApprovalsSearch.DateRangeKey,
                    [$"{FromDate.Value:d MMM yyyy} to {ToDate.Value:d MMM yyyy}"]);
            }
            else
            {
                // Only one date entered — show individual filter
                if (FromDate.HasValue)
                {
                    filters.Add(ApprovalsSearch.FromDateKey, [FromDate.Value.ToString("d MMM yyyy")]);
                }

                if (ToDate.HasValue)
                {
                    filters.Add(ApprovalsSearch.ToDateKey, [ToDate.Value.ToString("d MMM yyyy")]);
                }
            }

            if (FromSubmission.HasValue)
            {
                filters.Add(ApprovalsSearch.FromSubmissionKey, [FromDaysSinceSubmission]);
            }

            if (ToSubmission.HasValue)
            {
                filters.Add(ApprovalsSearch.ToSubmissionKey, [ToDaysSinceSubmission]);
            }

            if (LeadNation.Count != 0)
            {
                filters.Add(ApprovalsSearch.LeadNationKey, LeadNation);
            }

            if (ParticipatingNation.Count != 0)
            {
                filters.Add(ApprovalsSearch.ParticipatingNationKey, ParticipatingNation);
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