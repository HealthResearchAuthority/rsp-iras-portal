namespace Rsp.IrasPortal.Application.DTOs.Responses;

public record ProjectModificationAuditTrailResponse
{
    public IEnumerable<ProjectModificationAuditTrailDto> Items { get; set; } = null!;
    public int TotalCount { get; set; }
}