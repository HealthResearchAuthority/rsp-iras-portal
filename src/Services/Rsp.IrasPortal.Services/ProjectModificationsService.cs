using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

/// <summary>
/// Service implementation for managing project modifications.
/// Handles retrieval and creation of project modifications and their changes
/// by delegating to the IProjectModificationsServiceClient and mapping responses.
/// </summary>
public class ProjectModificationsService(IProjectModificationsServiceClient projectModificationsServiceClient) : IProjectModificationsService
{
    /// <summary>
    /// Gets the saved application by Id.
    /// </summary>
    /// <param name="projectRecordId">Project Record Id.</param>
    /// <param name="projectModificationId">Modification Id.</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    public async Task<ServiceResponse<ProjectModificationResponse>> GetModification(string projectRecordId, Guid projectModificationId)
    {
        var apiResponse = await projectModificationsServiceClient.GetModification(projectRecordId, projectModificationId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all the saved applications for a given project record.
    /// </summary>
    /// <param name="projectRecordId">Project Record Id.</param>
    /// <returns>An asynchronous operation that returns all the saved applications.</returns>
    public async Task<ServiceResponse<IEnumerable<ProjectModificationResponse>>> GetModifications(string projectRecordId)
    {
        var apiResponse = await projectModificationsServiceClient.GetModifications(projectRecordId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets the saved applications by Id and status.
    /// </summary>
    /// <param name="projectRecordId">Project Record Id.</param>
    /// <param name="status">Project Record Status.</param>
    /// <returns>An asynchronous operation that returns saved applications filtered by status.</returns>
    public async Task<ServiceResponse<IEnumerable<ProjectModificationResponse>>> GetModificationsByStatus(string projectRecordId, string status)
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationsByStatus(projectRecordId, status);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Creates a new project modification.
    /// </summary>
    /// <param name="projectModificationRequest">The request object containing details for the new modification.</param>
    /// <returns>An asynchronous operation that returns the newly created project modification.</returns>
    public async Task<ServiceResponse<ProjectModificationResponse>> CreateModification(ProjectModificationRequest projectModificationRequest)
    {
        var apiResponse = await projectModificationsServiceClient.CreateModification(projectModificationRequest);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Creates a new change for an existing project modification.
    /// </summary>
    /// <param name="projectModificationChangeRequest">The request object containing details for the modification change.</param>
    /// <returns>
    /// An asynchronous operation that returns a <see cref="ServiceResponse{ProjectModificationChangeResponse}"/>
    /// containing the result of the modification change creation.
    /// </returns>
    public async Task<ServiceResponse<ProjectModificationChangeResponse>> CreateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest)
    {
        var apiResponse = await projectModificationsServiceClient.CreateModificationChange(projectModificationChangeRequest);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all the area of changes and specific area of changes for the modification.
    /// </summary>
    /// /// <returns>
    /// An asynchronous operation that returns a <see cref="ServiceResponse{GetAreaOfChangesResponse}"/>
    /// containing the result of the area of changes.
    /// </returns>
    public async Task<ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>> GetAreaOfChanges()
    {
        var apiResponse = await projectModificationsServiceClient.GetAreaOfChanges();

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Creates one or more project modification documents based on the provided request data.
    /// </summary>
    /// <param name="projectModificationDocumentRequest">
    /// A list of <see cref="ProjectModificationDocumentRequest"/> objects containing the details of each document to be created,
    /// such as file metadata, modification change identifiers, and document type associations.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation,
    /// containing a <see cref="ServiceResponse"/> indicating success or failure of the creation process.
    /// </returns>
    public async Task<ServiceResponse> CreateDocumentModification(List<ProjectModificationDocumentRequest> projectModificationDocumentRequest)
    {
        var apiResponse = await projectModificationsServiceClient.CreateModificationDocument(projectModificationDocumentRequest);

        return apiResponse.ToServiceResponse();
    }
}