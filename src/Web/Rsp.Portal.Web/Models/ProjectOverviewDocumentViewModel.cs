using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class ProjectOverviewDocumentViewModel
{
    public ProjectDocumentsSearchModel Search { get; set; } = new();
    public IEnumerable<ProjectOverviewDocumentDto> Documents { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel? ProjectOverviewModel { get; set; }
}