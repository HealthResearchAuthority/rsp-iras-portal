using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ConfirmAddUpdateSponsorOrganisationUserModel
{
    public SponsorOrganisationModel SponsorOrganisation { get; set; } = null!;
    public UserViewModel User { get; set; } = null!;
    public bool IsRemove { get; set; } = false;
}