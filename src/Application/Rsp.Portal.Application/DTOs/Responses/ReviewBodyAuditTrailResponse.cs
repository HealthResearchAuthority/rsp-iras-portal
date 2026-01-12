namespace Rsp.Portal.Application.DTOs.Responses;

public class ReviewBodyAuditTrailResponse
{
    public IEnumerable<ReviewBodyAuditTrailDto> Items { get; set; } = Enumerable.Empty<ReviewBodyAuditTrailDto>();
    public int TotalCount { get; set; }
}