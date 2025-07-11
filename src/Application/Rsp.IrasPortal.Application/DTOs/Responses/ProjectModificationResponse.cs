namespace Rsp.IrasPortal.Application.DTOs.Responses;

/// <summary>
/// Represents the response data for a project modification, including identifiers, status, and audit information.
/// </summary>
public record ProjectModificationResponse
{
    /// <summary>
    /// The unique identifier for the project modification record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the related project record.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// The sequential number of the modification for the project.
    /// </summary>
    public int ModificationNumber { get; set; }

    /// <summary>
    /// The unique identifier string for the modification.
    /// </summary>
    public string ModificationIdentifier { get; set; } = null!;

    /// <summary>
    /// The current status of the project modification.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// The user ID of the person who created the modification.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// The user ID of the person who last updated the modification.
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// The date and time when the modification was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The date and time when the modification was last updated.
    /// </summary>
    public DateTime UpdatedDate { get; set; }
}