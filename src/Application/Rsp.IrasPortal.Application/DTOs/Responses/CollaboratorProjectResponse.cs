namespace Rsp.IrasPortal.Application.DTOs.Responses;

/// <summary>
/// Represents a collaborator's access level for a project.
/// </summary>
public class CollaboratorProjectResponse
{
    /// <summary>
    /// Gets or sets the project record identifier.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collaborator access level for the project.
    /// </summary>
    public string ProjectAccessLevel { get; set; } = null!;
}