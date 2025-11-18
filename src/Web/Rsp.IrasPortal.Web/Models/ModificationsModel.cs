namespace Rsp.IrasPortal.Web.Models;

public class ModificationsModel
{
    public string Id { get; set; } = null!;
    public string ProjectRecordId { get; set; } = null!;
    public string ModificationId { get; set; } = null!;
    public string ShortProjectTitle { get; set; } = null!;
    public string? ModificationType { get; set; } = null!;
    public string ChiefInvestigator { get; set; } = null!;
    public string LeadNation { get; set; } = null!;
    public string SponsorOrganisation { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int DaysSinceSubmission =>
        DateSubmitted.HasValue
            ? (DateTime.UtcNow.Date - DateSubmitted.Value.Date).Days
            : 0;

    public string Status { get; set; } = null!;
    public int ModificationNumber { get; set; }
    public DateTime? SentToSponsorDate { get; set; } = null;
    public DateTime? SentToRegulatorDate { get; set; } = null;
    public string ReviewerName { get; set; }
    // Virtual / computed field
    public DateTime? DateSubmitted => SentToRegulatorDate ?? SentToSponsorDate;
}