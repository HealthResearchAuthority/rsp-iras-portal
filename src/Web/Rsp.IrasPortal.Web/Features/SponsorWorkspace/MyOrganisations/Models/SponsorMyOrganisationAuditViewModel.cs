using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationAuditViewModel
{
    public string RtsId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public IEnumerable<SponsorOrganisationAuditTrailDto> AuditTrails { get; set; } = [];
    public PaginationViewModel Pagination { get; set; } = null!;
}