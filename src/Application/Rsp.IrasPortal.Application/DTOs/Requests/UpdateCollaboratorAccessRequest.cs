namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents the request payload used to update the access level of an existing project collaborator.
/// </summary>
public class UpdateCollaboratorAccessRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the collaborator record to update.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new access level to assign to the collaborator.
    /// </summary>
    public string ProjectAccessLevel { get; set; } = null!;
}