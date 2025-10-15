using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class SponsorOrganisationUserModel
{
    public SponsorOrganisationModel SponsorOrganisation { get; set; } = null!;
    public SponsorOrganisationUserDto SponsorOrganisationUser { get; set; } = null!;
    public UserViewModel User { get; set; } = null!;
}