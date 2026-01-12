using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class PostApprovalViewModel
{
    public ApprovalsSearchModel Search { get; set; } = new();
    public IEnumerable<PostApprovalModificationsModel> Modifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel? ProjectOverviewModel { get; set; }
    public IEnumerable<ProjectClosuresModel> ProjectClosureModels { get; set; } = [];
}