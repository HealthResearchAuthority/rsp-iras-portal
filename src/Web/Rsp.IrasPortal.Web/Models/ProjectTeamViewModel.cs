using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ProjectTeamViewModel
{
    public List<Collaborator> Collaborators { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel ProjectOverviewModel { get; set; } = null!;
}