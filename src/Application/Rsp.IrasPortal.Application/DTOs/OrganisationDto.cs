namespace Rsp.IrasPortal.Application.DTOs;

public record OrganisationDto
{
    /// <summary>
    /// Organisation Id
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Organisation Name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Organisation Address
    /// </summary>
    public string Address { get; set; } = null!;

    /// <summary>
    /// Organisation Country Name
    /// </summary>
    public string CountryName { get; set; } = null!;

    /// <summary>
    /// Organisation Type
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// Organisation Type
    /// </summary>
    public string LastUpdated { get; set; } = null!;
}