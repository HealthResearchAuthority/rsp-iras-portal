using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;

public class SelectedOrganisationViewModel
{
    public List<OrganisationModel> Organisations { get; set; } = [];

    public PaginationViewModel? Pagination { get; set; }
}