namespace Rsp.IrasPortal.Application.DTOs;

/// <summary>
///     Represents an individual search result returned by the RTS API.
/// </summary>
public record RtsSearchByNameDto
{
    /// <summary>
    ///     Unique identifier of the medical facility.
    /// </summary>
    public string Id { get; init; } = null!;

    /// <summary>
    ///     Name of the medical facility.
    /// </summary>
    public string Name { get; init; } = null!;
}