using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationAuditViewModel
{
    public string RtsId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public IEnumerable<SponsorOrganisationAuditTrailDto> AuditTrails { get; set; } = [];
}