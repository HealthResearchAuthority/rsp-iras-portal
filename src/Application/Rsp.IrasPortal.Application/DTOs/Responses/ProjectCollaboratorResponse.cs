namespace Rsp.IrasPortal.Application.DTOs.Responses;

/// <summary>
/// Represents a project collaborator returned by API and application queries.
/// </summary>
public class ProjectCollaboratorResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the collaborator record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the project record identifier the collaborator belongs to.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the collaborator user.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collaborator access level for the project.
    /// </summary>
    public string ProjectAccessLevel { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the current user is the owner of the project.
    /// </summary>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Gets or sets the UTC date when the collaborator was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the UTC date when the collaborator was last updated.
    /// </summary>
    public DateTime UpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the collaborator.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the collaborator.
    /// </summary>
    public string UpdatedBy { get; set; } = null!;
}