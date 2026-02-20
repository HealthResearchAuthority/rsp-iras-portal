using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

public class AuthorisationsModificationsViewModel
{
    public AuthorisationsModificationsSearchModel Search { get; set; } = new();
    public IEnumerable<ModificationsModel> Modifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public Guid SponsorOrganisationUserId { get; set; }
    public string RtsId { get; set; }
    public string SponsorOrganisationName { get; set; }

    public int SponsorOrgansationCount { get; set; }
}