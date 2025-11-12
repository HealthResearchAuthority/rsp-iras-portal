using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class ApprovalsSearchModel
{
    public string? IrasId { get; set; }
    public string? ChiefInvestigatorName { get; set; }
    public string? ShortProjectTitle { get; set; }
    public string? SponsorOrganisation { get; set; }

    public DateTime? FromDate => DateTimeExtensions.ParseDateValidation(FromDay, FromMonth, FromYear);
    public DateTime? ToDate => DateTimeExtensions.ParseDateValidation(ToDay, ToMonth, ToYear);

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

    public string? ModificationType { get; set; }
    public string? ReviewType { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? ModificationId { get; set; }
    public string? ReviewerName { get; set; }

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
            if (!string.IsNullOrWhiteSpace(ModificationType))
            {
                filters.Add(ApprovalsSearch.ModificationTypeKey, [ModificationType]);
            }
            if (!string.IsNullOrWhiteSpace(ReviewType))
            {
                filters.Add(ApprovalsSearch.ReviewTypeKey, [ReviewType]);
            }
            if (!string.IsNullOrWhiteSpace(Category))
            {
                filters.Add(ApprovalsSearch.CategoryKey, [Category]);
            }
            if (!string.IsNullOrWhiteSpace(Status))
            {
                filters.Add(ApprovalsSearch.StatusKey, [Status]);
            }
            if (!string.IsNullOrWhiteSpace(ReviewerName))
            {
                filters.Add(ApprovalsSearch.ReviewerNameKey, [ReviewerName]);
            }
            return filters;
        }
    }

    public bool IgnoreFilters { get; set; }
}