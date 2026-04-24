using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.Portal.Application.ServiceClients;

/// <summary>
/// Interface to interact with Applications microservice
/// </summary>
public interface IProjectCollaboratorServiceClient
{
    /// <summary>
    /// Retrieves all collaborators associated with a specific project.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project.</param>
    /// <returns>A collection of project collaborators.</returns>
    [Get("/projectcollaborators/getprojectcollaborators")]
    Task<IApiResponse<IEnumerable<ProjectCollaboratorResponse>>> GetProjectCollaborators(string projectRecordId);

    /// <summary>
    /// Saves a new project collaborator or updates an existing one.
    /// </summary>
    /// <param name="projectCollaboratorRequest">The collaborator details to save.</param>
    /// <returns>The saved project collaborator.</returns>
    [Post("/projectcollaborators/saveprojectcollaborator")]
    Task<IApiResponse<ProjectCollaboratorResponse>> SaveProjectCollaborator([Body] ProjectCollaboratorRequest projectCollaboratorRequest);

    /// <summary>
    /// Removes a collaborator from a project.
    /// </summary>
    /// <param name="projectCollaboratorId">The unique identifier of the project collaborator to remove.</param>
    /// <returns>An API response indicating the result of the operation.</returns>
    [Delete("/projectcollaborators/removecollaborator")]
    Task<IApiResponse> RemoveProjectCollaborator(string projectCollaboratorId);

    /// <summary>
    /// Retrieves all projects associated with a specific user as a collaborator.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A collection of projects where the user is a collaborator.</returns>
    [Get("/projectcollaborators/getcollaboratorprojects")]
    Task<IApiResponse<IEnumerable<CollaboratorProjectResponse>>> GetCollaboratorProjects(string userId);

    /// <summary>
    /// Updates the access level of an existing project collaborator.
    /// </summary>
    /// <param name="updateCollaboratorAccessRequest">The update payload containing the collaborator identifier and new access level.</param>
    /// <returns>An API response indicating the result of the operation.</returns>
    [Patch("/projectcollaborators/updatecollaboratoraccess")]
    Task<IApiResponse> UpdateCollaboratorAccess([Body] UpdateCollaboratorAccessRequest updateCollaboratorAccessRequest);
}