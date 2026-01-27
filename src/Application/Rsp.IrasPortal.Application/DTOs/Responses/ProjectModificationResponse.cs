namespace Rsp.Portal.Application.DTOs.Responses;

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
    /// The reason for rejecting approval.
    /// </summary>
    public string? ReasonNotApproved { get; set; } = null!;

    /// <summary>
    /// The reviewer comments.
    /// </summary>
    public string? ReviewerComments { get; set; } = null!;

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

    /// <summary>
    /// Overall ranking type of the modification
    /// </summary>
    public string? ModificationType { get; set; }

    /// <summary>
    /// Overall ranking category of the modification
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Overall ranking review type of the modification
    /// </summary>
    public string? ReviewType { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the reviewer assigned to this modification, if any.
    /// </summary>
    public string? ReviewerId { get; set; }

    /// <summary>
    /// Gets or sets the email of the reviewer assigned to this modification, if any.
    /// </summary>
    public string? ReviewerEmail { get; set; }

    /// <summary>
    /// Gets or sets the name of the reviewer assigned to this modification, if any.
    /// </summary>
    public string? ReviewerName { get; set; }

    /// <summary>
    /// Gets or sets the submission date.
    /// This date is populated when a researcher clicks send to sponsor from the Reveiw all changes page, the actual status is With Sponsor
    /// </summary>
    public DateTime? SentToSponsorDate { get; set; }

    public DateTime? SentToRegulatorDate { get; set; }
}