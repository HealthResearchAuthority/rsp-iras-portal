using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class ReviewBodyAuditTrailViewModel
{
    public string? BodyName { get; set; }
    public PaginationViewModel Pagination { get; set; } = null!;
    public IEnumerable<ReviewBodyAuditTrailDto> Items { get; set; } = Enumerable.Empty<ReviewBodyAuditTrailDto>();
}