namespace Rsp.IrasPortal.Web.Models;

public class ApprovalsModificationViewModel
{
    public string ModificationId { get; set; } = null!;
    public string? ShortProjectTite { get; set; }
    public string? ModificationType { get; set; }
    public string? ChiefInvestigator { get; set; }
    public string? LeadNation { get; set; }
    public string? SponsorOrganisation { get; set; }
    public DateOnly? Date { get; set; }
}