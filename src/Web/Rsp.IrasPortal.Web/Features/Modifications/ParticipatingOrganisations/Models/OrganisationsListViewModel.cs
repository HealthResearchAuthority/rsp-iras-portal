using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;

public class OrganisationsListViewModel : BaseProjectModificationViewModel
{
    public List<ParticipatingOrganisationModel> Organisations { get; set; } = [];
}