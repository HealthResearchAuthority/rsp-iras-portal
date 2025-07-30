namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to change a project modification, including details about the area of change,
/// status, and user information.
/// </summary>
public record ProjectModificationChangeRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the project modification record.
    /// </summary>
    public Guid ProjectModificationId { get; set; }

    /// <summary>
    /// Gets or sets the general area where the change is being made.
    /// </summary>
    public string AreaOfChange { get; set; } = null!;

    /// <summary>
    /// Gets or sets the specific area within the general area where the change is being made.
    /// </summary>
    public string SpecificAreaOfChange { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current status of the project modification.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user identifier of the person who created the project modification request.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user identifier of the person who last updated the project modification request.
    /// </summary>
    public string UpdatedBy { get; set; } = null!;
}