namespace Rsp.Portal.Application.DTOs.Responses;

public class AllSponsorOrganisationsResponse
{
    public IEnumerable<SponsorOrganisationDto> SponsorOrganisations { get; set; } = [];

    public int TotalCount { get; set; }
}