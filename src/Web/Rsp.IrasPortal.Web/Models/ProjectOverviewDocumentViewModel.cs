using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ProjectOverviewDocumentViewModel
{
    public ProjectDocumentsSearchModel Search { get; set; } = new();
    public IEnumerable<ProjectOverviewDocumentDto> Documents { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel? ProjectOverviewModel { get; set; }
}