namespace Rsp.IrasPortal.Application.DTOs.Responses;

/// <summary>
/// Represents the response for organisation search via RTS.
/// </summary>
public class OrganisationSearchResponse
{
    /// <summary>
    /// Organisation Id
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Organisation Name
    /// </summary>
    public string Name { get; set; } = null!;
}