using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ModificationsTasklistViewModel
{
    public ApprovalsSearchModel Search { get; set; } = new();
    public IEnumerable<TaskListModificationViewModel> Modifications { get; set; } = [];
    public List<string> SelectedModificationIds { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public bool EmptySearchPerformed { get; set; } = false;
    public string LeadNation { get; set; }
}