using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;

public class ParticipatingOrganisationModel : OrganisationModel
{
    public Guid OrganisationId { get; set; }

    public string DetailsStatus { get; set; } = null!;
}