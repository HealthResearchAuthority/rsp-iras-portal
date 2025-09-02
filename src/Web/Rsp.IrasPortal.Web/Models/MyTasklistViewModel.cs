using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class MyTasklistViewModel
{
    public ApprovalsSearchModel Search { get; set; } = new();
    public IEnumerable<ModificationsModel>? Modifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public bool EmptySearchPerformed { get; set; } = false;
}