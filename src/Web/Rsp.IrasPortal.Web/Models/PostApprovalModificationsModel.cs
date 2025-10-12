using Rsp.IrasPortal.Application.Enums;

namespace Rsp.IrasPortal.Web.Models;

public class PostApprovalModificationsModel
{
    public string ModificationId { get; set; } = null!;
    public string ModificationIdentifier { get; set; } = null!;
    public string? ModificationType { get; set; } = null!;
    public string? ReviewType { get; set; } = null!;
    public string? Category { get; set; } = null!;
    public DateTime? DateSubmitted { get; set; }
    public string? Status { get; set; } = null!;
    public ModificationStatusOrder? StatusOrder { get; set; } = null!;
    public DateTime? SubmittedDate { get; set; }
}