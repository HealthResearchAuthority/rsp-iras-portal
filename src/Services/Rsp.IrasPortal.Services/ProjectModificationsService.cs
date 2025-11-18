using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
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
public class ProjectModificationsService
(
    IProjectModificationsServiceClient projectModificationsServiceClient
) : IProjectModificationsService
{
    /// <summary>
    /// Gets the saved application by Id.
    /// </summary>
    /// <param name="projectModificationId">Modification Id.</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    public async Task<ServiceResponse<ProjectModificationResponse>> GetModification(Guid projectModificationId)
    {
        var apiResponse = await projectModificationsServiceClient.GetModification(projectModificationId);
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
    /// Gets all modifications with filtering, sorting and pagination
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications matching the search criteria.</returns>
    public async Task<ServiceResponse<GetModificationsResponse>> GetModifications
    (
        ModificationSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsDto.ModificationId),
        string sortDirection = SortDirections.Descending
    )
    {
        var apiResponse = await projectModificationsServiceClient.GetModifications(searchQuery, pageNumber, pageSize, sortField, sortDirection);

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
    /// Gets modifications for specific ProjectRecordId with filtering, sorting and pagination
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record for which modifications are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications related to the specified project record.</returns>
    public async Task<ServiceResponse<GetModificationsResponse>> GetModificationsForProject
    (
        string projectRecordId,
        ModificationSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsDto.ModificationId),
        string sortDirection = SortDirections.Descending
    )
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationsForProject(projectRecordId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Retrieves modifications by a list of modification IDs.
    /// </summary>
    /// <param name="Ids">A list of IDs relating to modifications</param>
    /// <returns>A list of modifications corresponding to the provided IDs</returns>
    public async Task<ServiceResponse<GetModificationsResponse>> GetModificationsByIds(List<string> Ids)
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationsByIds(Ids);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets modifications for specific sponsorOrganisationUserId with filtering, sorting and pagination
    /// </summary>
    /// <param name="sponsorOrganisationUserId">The unique identifier of the sponsor organisation user for which modifications are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    public async Task<ServiceResponse<GetModificationsResponse>> GetModificationsBySponsorOrganisationUserId
    (
       Guid sponsorOrganisationUserId,
       SponsorAuthorisationsSearchRequest searchQuery,
       int pageNumber = 1,
       int pageSize = 20,
       string sortField = nameof(ModificationsDto.SentToSponsorDate),
       string sortDirection = SortDirections.Descending
    )
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationsBySponsorOrganisationUserId(sponsorOrganisationUserId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

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

    public async Task<ServiceResponse> AssignModificationsToReviewer(List<string> modificationIds, string reviewerId, string reviewerEmail, string reviewerName)
    {
        var apiResponse = await projectModificationsServiceClient.AssignModificationsToReviewer(modificationIds, reviewerId, reviewerEmail, reviewerName);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets modifications for specific ProjectRecordId with filtering, sorting and pagination
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record for which modifications are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications related to the specified project record.</returns>
    public async Task<ServiceResponse<ProjectOverviewDocumentResponse>> GetDocumentsForProjectOverview(string projectRecordId, ProjectOverviewDocumentSearchRequest searchQuery, int pageNumber = 1, int pageSize = 20, string sortField = "DocumentType", string sortDirection = "desc")
    {
        var apiResponse = await projectModificationsServiceClient.GetDocumentsForProjectOverview(projectRecordId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ProjectModificationChangeResponse>> GetModificationChange(Guid modificationChangeId)
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationChange(modificationChangeId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>> GetModificationChanges(Guid projectModificationId)
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationChanges(projectModificationId);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Removes a change from an existing project modification by its unique identifier.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier of the project modification change to remove.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="ServiceResponse"/>
    /// that reflects the success or failure of the deletion operation.
    /// </returns>
    public async Task<ServiceResponse> RemoveModificationChange(Guid modificationChangeId)
    {
        // Invoke microservice client to delete the modification change.
        var apiResponse = await projectModificationsServiceClient.RemoveModificationChange(modificationChangeId);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Updates sttaus of an existing project modification by its unique identifier.
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project modification to update.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="ServiceResponse"/>
    /// that reflects the success or failure of the update operation.
    /// </returns>
    public async Task<ServiceResponse> UpdateModificationStatus(Guid modificationId, string status)
    {
        // Invoke microservice client to delete the modification change.
        var apiResponse = await projectModificationsServiceClient.UpdateModificationStatus(modificationId, status);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Deletes one or more project modification documents based on the provided request data.
    /// </summary>
    /// <param name="projectModificationDocumentRequest">
    /// A list of <see cref="ProjectModificationDocumentRequest"/> objects containing the details of each document to be created,
    /// such as file metadata, modification change identifiers, and document type associations.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation,
    /// containing a <see cref="ServiceResponse"/> indicating success or failure of the creation process.
    /// </returns>
    public async Task<ServiceResponse> DeleteDocumentModification(List<ProjectModificationDocumentRequest> projectModificationDocumentRequest)
    {
        var apiResponse = await projectModificationsServiceClient.DeleteDocuments(projectModificationDocumentRequest);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Deletes existing project modification by its unique identifier.
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project modification to update.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="ServiceResponse"/>
    /// that reflects the success or failure of the update operation.
    /// </returns>
    public async Task<ServiceResponse> DeleteModification(Guid modificationId)
    {
        // Invoke microservice client to delete the modification change.
        var apiResponse = await projectModificationsServiceClient.DeleteModification(modificationId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ProjectModificationAuditTrailResponse>> GetModificationAuditTrail(Guid modificationId)
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationAuditTrail(modificationId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest)
    {
        // Invoke microservice client to update the modification change.
        var apiResponse = await projectModificationsServiceClient.UpdateModificationChange(projectModificationChangeRequest);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateModification(ProjectModificationRequest projectModificationRequest)
    {
        // Invoke microservice client to update the modification.
        var apiResponse = await projectModificationsServiceClient.UpdateModification(projectModificationRequest);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Saves review responses for a project modification.
    /// </summary>
    /// <param name="modificationReviewRequest">The request object containing the review values</param>
    public async Task<ServiceResponse> SaveModificationReviewResponses(ProjectModificationReviewRequest modificationReviewRequest)
    {
        var apiResponse = await projectModificationsServiceClient.SaveModificationReviewResponses(modificationReviewRequest);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets review responses for a project modification.
    /// </summary>
    /// <param name="modificationId">The ID of the modification</param>
    /// <returns>Returns the modification review properties</returns>
    public async Task<ServiceResponse<ProjectModificationReviewResponse>> GetModificationReviewResponses(Guid modificationId)
    {
        var apiResponse = await projectModificationsServiceClient.GetModificationReviewResponses(modificationId);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets modifications for specific ProjectRecordId with filtering, sorting and pagination
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project record for which modifications are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications related to the specified project record.</returns>
    public async Task<ServiceResponse<ProjectOverviewDocumentResponse>> GetDocumentsForModification(Guid modificationId, ProjectOverviewDocumentSearchRequest searchQuery, int pageNumber = 1, int pageSize = 20, string sortField = "DocumentType", string sortDirection = "desc")
    {
        var apiResponse = await projectModificationsServiceClient.GetDocumentsForModification(modificationId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        return apiResponse.ToServiceResponse();
    }
}