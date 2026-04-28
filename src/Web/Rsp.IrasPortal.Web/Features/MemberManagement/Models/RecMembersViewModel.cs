using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Features.MemberManagement.Models;

public class RecMembersViewModel
{
    public string? RecName { get; set; }
    public Guid? RecId { get; set; }
    public IEnumerable<RecMemberViewModel>? RecUsers { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}