using Rsp.IrasPortal.Application.Enum;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class PostApprovalViewModel
{
    public ApprovalsSearchModel Search { get; set; } = new();
    public IEnumerable<PostApprovalModificationsModel> Modifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel? ProjectOverviewModel { get; set; }
    public IEnumerable<ProjectClosuresModel> ProjectClosureModels { get; set; } = [];
    public IEnumerable<ProjectModificationChangeResponse> AllProjectModificationChanges { get; set; } = [];

    //Validate any modifications are in in trsaction status if any restrict create new modification.
    public ModificationCreationCheckResult CanCreateNewModification()
    {
        if (Modifications.Any(m => ModificationStatus.InTransactionStatus.Contains(m.Status)))
        {
            return ModificationCreationCheckResult.InvalidStatus;
        }

        if (AllProjectModificationChanges.Any(change => change.Status is ModificationStatus.WithReviewBody && BlockedSpecificAreas.Contains(change.SpecificAreaOfChange)))
        {
            return ModificationCreationCheckResult.BlockedSpecificAreaOfChange;
        }

        return ModificationCreationCheckResult.Success;
    }

    private readonly List<string> BlockedSpecificAreas = [AreasOfChange.ProjectHalt, AreasOfChange.ChangeOfPrimarySponsor];
}