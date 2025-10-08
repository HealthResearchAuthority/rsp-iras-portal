using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class SponsorOrganisationSearchViewModel
{
    public SponsorOrganisationSearchModel Search { get; set; } = new();
    public IEnumerable<SponsorOrganisationDto> SponsorOrganisations { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}