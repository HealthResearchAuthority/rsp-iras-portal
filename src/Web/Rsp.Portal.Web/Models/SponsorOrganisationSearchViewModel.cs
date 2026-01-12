using System.Diagnostics.CodeAnalysis;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

[ExcludeFromCodeCoverage]
public class SponsorOrganisationSearchViewModel
{
    public SponsorOrganisationSearchModel Search { get; set; } = new();
    public IEnumerable<SponsorOrganisationDto> SponsorOrganisations { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}