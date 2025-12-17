using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationsViewModel
{
    public SponsorMyOrganisationsSearchModel Search { get; set; } = new();
    public IEnumerable<SponsorMyOrganisationModel> MyOrganisations { get; set; } = [];
    public Guid SponsorOrganisationUserId { get; set; }
}