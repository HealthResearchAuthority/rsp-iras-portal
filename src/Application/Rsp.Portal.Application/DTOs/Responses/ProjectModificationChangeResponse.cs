namespace Rsp.Portal.Application.DTOs.Responses;

/// <summary>
/// Represents a response DTO containing information about a project modification change.
/// </summary>
public record ProjectModificationChangeResponse
{
    /// <summary>
    /// The unique identifier for the project modification change record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier for the related project modification.
    /// </summary>
    public Guid ProjectModificationId { get; set; }

    /// <summary>
    /// The main area of change for the project.
    /// </summary>
    public string AreaOfChange { get; set; } = null!;

    /// <summary>
    /// The specific area within the main area of change.
    /// </summary>
    public string SpecificAreaOfChange { get; set; } = null!;

    /// <summary>
    /// The current status of the project modification change.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Ranking type of the change
    /// </summary>
    public string? ModificationType { get; set; }

    /// <summary>
    /// Ranking category of the change
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Ranking review type of the change
    /// </summary>
    public string? ReviewType { get; set; }

    /// <summary>
    /// The user identifier of the person who created the modification change.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// The user identifier of the person who last updated the modification change.
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// The date and time when the modification change was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The date and time when the modification change was last updated.
    /// </summary>
    public DateTime UpdatedDate { get; set; }
}