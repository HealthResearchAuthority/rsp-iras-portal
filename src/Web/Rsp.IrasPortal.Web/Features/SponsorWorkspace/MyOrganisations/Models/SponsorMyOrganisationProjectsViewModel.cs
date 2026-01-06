using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationProjectsViewModel
{
    public string Name { get; set; }
    public string RtsId { get; set; }

    public IEnumerable<CompleteProjectRecordResponse> ProjectRecords { get; set; } = new List<CompleteProjectRecordResponse>();
    public SponsorOrganisationProjectSearchModel Search { get; set; } = new();
    public PaginationViewModel? Pagination { get; set; }
}