namespace Rsp.Portal.Application.DTOs.Responses;

/// <summary>
/// Represents the response for organisation search via RTS.
/// </summary>
public class OrganisationSearchResponse
{
    public List<OrganisationDto> Organisations { get; set; } = [];

    public int TotalCount { get; set; }
}