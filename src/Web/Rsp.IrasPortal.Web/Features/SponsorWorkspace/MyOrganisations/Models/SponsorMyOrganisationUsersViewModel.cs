using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationUsersViewModel
{
    public string Name { get; set; }
    public string RtsId { get; set; }
    public string Role { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public bool CanAuthorise { get; set; }
    public bool IsCurrentUserAdmin { get; set; } = false;

    public IEnumerable<UserViewModel> Users { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public SponsorOrganisationModel SponsorOrganisation { get; set; } = null!;
}