using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;

public class AuthorisationsModificationsViewModel
{
    public AuthorisationsModificationsSearchModel Search { get; set; } = new();
    public IEnumerable<ModificationsModel> Modifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public Guid SponsorOrganisationUserId { get; set; }
}