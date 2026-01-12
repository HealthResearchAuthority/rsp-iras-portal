using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class SponsorOrganisationAuditTrailViewModel
{
    public string? SponsorOrganisation { get; set; }
    public string? RtsId { get; set; }
    public PaginationViewModel Pagination { get; set; } = null!;
    public IEnumerable<SponsorOrganisationAuditTrailDto> Items { get; set; } = Enumerable.Empty<SponsorOrganisationAuditTrailDto>();
}