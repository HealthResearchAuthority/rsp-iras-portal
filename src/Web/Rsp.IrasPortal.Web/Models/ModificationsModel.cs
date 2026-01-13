namespace Rsp.Portal.Web.Models;

public class ModificationsModel
{
    public string Id { get; set; } = null!;
    public string ProjectRecordId { get; set; } = null!;
    public string ModificationId { get; set; } = null!;
    public string ShortProjectTitle { get; set; } = null!;
    public string? ModificationType { get; set; } = null!;
    public string ChiefInvestigatorFirstName { get; set; } = null!;
    public string ChiefInvestigatorLastName { get; set; } = null!;
    public string ChiefInvestigator { get; set; } = null!;
    public string LeadNation { get; set; } = null!;
    public string SponsorOrganisation { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int DaysSinceSubmission =>
        SentToRegulatorDate.HasValue
            ? (DateTime.UtcNow.Date - SentToRegulatorDate.Value.Date).Days
            : 0;

    public string Status { get; set; } = null!;
    public int ModificationNumber { get; set; }
    public DateTime? SentToSponsorDate { get; set; } = null;
    public DateTime? SentToRegulatorDate { get; set; } = null;
    public string ReviewerName { get; set; }
}