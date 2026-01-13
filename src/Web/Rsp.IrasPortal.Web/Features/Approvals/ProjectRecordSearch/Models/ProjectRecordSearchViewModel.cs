using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Approvals.ProjectRecordSearch.Models;

public class ProjectRecordSearchViewModel
{
    public IEnumerable<CompleteProjectRecordResponse> Applications { get; set; } = new List<CompleteProjectRecordResponse>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public ApprovalsSearchModel Search { get; set; } = new();
    public PaginationViewModel? Pagination { get; set; }
    public bool EmptySearchPerformed { get; set; } = false;
}