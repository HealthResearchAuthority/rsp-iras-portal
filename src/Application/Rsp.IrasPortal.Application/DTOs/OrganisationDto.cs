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
}