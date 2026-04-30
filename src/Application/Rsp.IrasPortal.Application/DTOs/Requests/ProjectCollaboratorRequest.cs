namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents the request payload used to create or update a project collaborator.
/// </summary>
public class ProjectCollaboratorRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the collaborator. If omitted, a new collaborator is created.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the project record identifier the collaborator belongs to.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the collaborator user.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the email of the collaborator user.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collaborator access level for the project.
    /// </summary>
    public string ProjectAccessLevel { get; set; } = null!;
}