namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to modify a project, including identifiers, status, and user information.
/// </summary>
public record ProjectModificationRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the project record.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier for the modification.
    /// </summary>
    public string ModificationIdentifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current status of the application.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID of the person who created the modification request.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID of the person who last updated the modification request.
    /// </summary>
    public string UpdatedBy { get; set; } = null!;
}