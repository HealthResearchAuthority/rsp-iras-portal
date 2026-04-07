using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;

public class SelectedOrganisationsViewModel : BaseProjectModificationViewModel
{
    public List<ParticipatingOrganisationModel> SelectedOrganisations { get; set; } = [];

    public PaginationViewModel? Pagination { get; set; }
}