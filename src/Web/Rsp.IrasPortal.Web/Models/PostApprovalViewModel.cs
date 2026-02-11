using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class PostApprovalViewModel
{
    public ApprovalsSearchModel Search { get; set; } = new();
    public IEnumerable<PostApprovalModificationsModel> Modifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel? ProjectOverviewModel { get; set; }
    public IEnumerable<ProjectClosuresModel> ProjectClosureModels { get; set; } = [];

    //Validate any modifications are in in trsaction status if any restrict create new modification.
    public bool CanCreateNewModification() =>
                !Modifications.Any(m => ModificationStatus.InTransactionStatus.Contains(m.Status));
}