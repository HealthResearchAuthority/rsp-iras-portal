using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ReviewBodyAuditTrailViewModel
{
    public string? BodyName { get; set; }
    public PaginationViewModel Pagination { get; set; } = null!;
    public IEnumerable<ReviewBodyAuditTrailDto> Items { get; set; } = Enumerable.Empty<ReviewBodyAuditTrailDto>();
}