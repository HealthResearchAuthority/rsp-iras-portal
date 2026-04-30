using Rsp.IrasPortal.Web.Features.ProjectCollaborators.Models;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ProjectTeamViewModel
{
    public List<CollaboratorViewModel> Collaborators { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel ProjectOverviewModel { get; set; } = null!;
}