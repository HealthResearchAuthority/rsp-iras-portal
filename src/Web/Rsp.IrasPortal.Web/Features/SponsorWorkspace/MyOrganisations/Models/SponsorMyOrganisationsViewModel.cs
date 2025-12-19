using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationsViewModel
{
    public SponsorMyOrganisationsSearchModel Search { get; set; } = new();
    public List<SponsorOrganisationDto> MyOrganisations { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}