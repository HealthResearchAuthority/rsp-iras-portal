namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class SponsorOrganisationAuditTrailResponse
{
    public IEnumerable<SponsorOrganisationAuditTrailDto> Items { get; set; } = Enumerable.Empty<SponsorOrganisationAuditTrailDto>();
    public int TotalCount { get; set; }
}