namespace Rsp.IrasPortal.Web.Models;

public class PostApprovalModificationsModel
{
    public string ModificationId { get; set; } = null!;
    public string ModificationIdentifier { get; set; } = null!;
    public string? ModificationType { get; set; } = null!;
    public string? ReviewType { get; set; } = null!;
    public string? Category { get; set; } = null!;
    public DateTime? SentToRegulatorDate { get; set; }
    public string? Status { get; set; } = null!;
    public DateTime? SentToSponsorDate { get; set; }
    public int ModificationNumber { get; set; }
}