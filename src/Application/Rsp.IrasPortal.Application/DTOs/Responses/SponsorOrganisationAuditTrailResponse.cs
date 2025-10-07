namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class SponsorOrganisationAuditTrailResponse
{
    public IEnumerable<ReviewBodyAuditTrailDto> Items { get; set; } = Enumerable.Empty<ReviewBodyAuditTrailDto>();
    public int TotalCount { get; set; }
}