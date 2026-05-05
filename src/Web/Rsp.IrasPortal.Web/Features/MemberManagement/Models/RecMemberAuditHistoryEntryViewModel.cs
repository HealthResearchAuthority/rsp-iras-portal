namespace Rsp.IrasPortal.Web.Features.MemberManagement.Models;

public class RecMemberAuditHistoryEntryViewModel
{
    public DateTime DateTimeStamp { get; set; }
    public string Description { get; set; } = null!;
    public string User { get; set; } = null!;
}