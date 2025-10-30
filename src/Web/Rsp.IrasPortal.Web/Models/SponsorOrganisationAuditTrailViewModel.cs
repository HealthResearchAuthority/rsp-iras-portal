using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class SponsorOrganisationAuditTrailViewModel
{
    public string? SponsorOrganisation { get; set; }
    public string? RtsId { get; set; }
    public PaginationViewModel Pagination { get; set; } = null!;
    public IEnumerable<SponsorOrganisationAuditTrailDto> Items { get; set; } = Enumerable.Empty<SponsorOrganisationAuditTrailDto>();
}