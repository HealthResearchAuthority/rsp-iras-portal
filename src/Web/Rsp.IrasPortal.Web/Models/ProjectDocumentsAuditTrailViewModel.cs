using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class ProjectDocumentsAuditTrailViewModel
{
    public IEnumerable<ModificationDocumentsAuditTrailDto> AuditTrails { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public ProjectOverviewModel? ProjectOverviewModel { get; set; }
}