using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationProjectsViewModel
{
    public string Name { get; set; }
    public string RtsId { get; set; }

    public IEnumerable<CompleteProjectRecordResponse> ProjectRecords { get; set; } = new List<CompleteProjectRecordResponse>();
    public SponsorOrganisationProjectSearchModel Search { get; set; } = new();
    public PaginationViewModel? Pagination { get; set; }
}