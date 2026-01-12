using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class ApprovalsSearchViewModel
{
    public ApprovalsSearchModel Search { get; set; } = new();
    public IEnumerable<ModificationsModel> Modifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public bool EmptySearchPerformed { get; set; } = false;
}