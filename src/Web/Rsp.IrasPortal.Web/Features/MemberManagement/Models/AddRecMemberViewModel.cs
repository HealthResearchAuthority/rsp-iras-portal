using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Features.MemberManagement.Models;

public class AddRecMemberViewModel
{
    public Guid RecId { get; set; }
    public string RecName { get; set; } = null!;
    public string? Email { get; set; }
    public IEnumerable<UserViewModel> Users { get; set; } = [];
}