using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Features.MemberManagement.Models;

public class RecMemberAuditHistoryViewModel
{
    public ReviewBodyDto ReviewBody { get; set; } = null!;
    public User User { get; set; } = null!;
    public List<RecMemberAuditHistoryEntryViewModel> AuditHistoryEntries { get; set; } = [];
    public PaginationViewModel Pagination { get; set; } = null!;
}