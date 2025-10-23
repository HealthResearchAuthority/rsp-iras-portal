namespace Rsp.IrasPortal.Application.DTOs;

/// <summary>
/// Data transfer object representing a project record within the IRAS Portal application.
/// </summary>
public record ProjectRecordDto
{
    /// <summary>
    /// Optional IRAS project identifier.
    /// </summary>
    public int? IrasId { get; set; }

    /// <summary>
    /// Optional record identifier from the source system.
    /// </summary>
    public int? RecID { get; set; }

    /// <summary>
    /// Optional record name or display name.
    /// </summary>
    public string? RecName { get; set; }

    /// <summary>
    /// Optional short project title for concise displays.
    /// </summary>
    public string? ShortProjectTitle { get; set; }

    /// <summary>
    /// Optional long or descriptive project title.
    /// </summary>
    public string? LongProjectTitle { get; set; }
}