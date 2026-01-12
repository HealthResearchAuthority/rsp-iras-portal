using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class SponsorOrganisationListUsersModel
{
    public IEnumerable<UserViewModel> Users { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public SponsorOrganisationModel SponsorOrganisation { get; set; } = null!;
}