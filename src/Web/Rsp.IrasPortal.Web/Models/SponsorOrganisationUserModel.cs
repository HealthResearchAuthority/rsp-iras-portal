using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class SponsorOrganisationUserModel
{
    public SponsorOrganisationModel SponsorOrganisation { get; set; } = null!;
    public SponsorOrganisationUserDto SponsorOrganisationUser { get; set; } = null!;
    public UserViewModel User { get; set; } = null!;
}