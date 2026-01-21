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

    //Validate new modification. Only one active in draft modification.
    public bool CanCreateNewModification() =>
        !Modifications.Any(m => m.Status == ModificationStatus.InDraft);

    //Validate modification while sending to sponsor.Only one in flight modofocation should exist.
    public bool CanModificationSendToSponsor() =>
        !Modifications.Any(m =>
            m.Status == ModificationStatus.WithSponsor ||
            m.Status == ModificationStatus.WithReviewBody);
}