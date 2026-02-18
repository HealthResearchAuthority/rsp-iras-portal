using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

public class ProjectClosuresViewModel
{
    public ProjectClosuresSearchModel Search { get; set; } = new();
    public IEnumerable<ProjectClosuresModel> ProjectRecords { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public Guid SponsorOrganisationUserId { get; set; }
    public string RtsId { get; set; }
    public string SponsorOrganisationName { get; set; }
    public int SponsorOrgansationCount { get; set; }
}