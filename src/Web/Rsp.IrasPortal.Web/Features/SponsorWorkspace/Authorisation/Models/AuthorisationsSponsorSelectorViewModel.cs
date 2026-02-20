using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

public class AuthorisationsSponsorSelectorViewModel
{
    public Guid SponsorOrganisationUserId { get; set; }
    public Guid RtsId { get; set; }
    public IEnumerable<SponsorOrganisationDto> SponsorOrganisations { get; set; }
}