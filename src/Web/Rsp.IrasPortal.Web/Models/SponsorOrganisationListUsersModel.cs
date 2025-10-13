using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class SponsorOrganisationListUsersModel
{
    public IEnumerable<UserViewModel> Users { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public SponsorOrganisationModel SponsorOrganisation { get; set; } = null!;
}