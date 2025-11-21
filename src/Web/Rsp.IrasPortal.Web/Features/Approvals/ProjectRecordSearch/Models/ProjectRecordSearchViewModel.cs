using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Approvals.ProjectRecordSearch.Models;

public class ProjectRecordSearchViewModel
{
    public IEnumerable<CompleteProjectRecordResponse> Applications { get; set; } = new List<CompleteProjectRecordResponse>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public ApprovalsSearchModel Search { get; set; } = new();
    public PaginationViewModel? Pagination { get; set; }
    public bool EmptySearchPerformed { get; set; } = false;
}