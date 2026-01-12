using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationAuditViewModel
{
    public string RtsId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public IEnumerable<SponsorOrganisationAuditTrailDto> AuditTrails { get; set; } = [];
}