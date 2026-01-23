using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationsViewModel
{
    public SponsorMyOrganisationsSearchModel Search { get; set; } = new();
    public List<SponsorOrganisationDto> MyOrganisations { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}