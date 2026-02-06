using Rsp.Logging.Interceptors;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;

namespace Rsp.Portal.Application.Services;

/// <summary>
/// Service interface for managing project modifications.
/// Marked as IInterceptable to enable start/end logging for all methods.
/// </summary>
public interface IProjectModificationsService : IInterceptable
{
    /// <summary>
    /// Retrieves a specific project modification by project record ID and modification ID.
    /// </summary>
    /// <param name="projectModificationId">The unique identifier of the project modification.</param>
    /// <returns>A service response containing the project modification details.</returns>
    Task<ServiceResponse<ProjectModificationResponse>> GetModification(string projectRecordId, Guid projectModificationId);

    /// <summary>
    /// Retrieves all modifications for a given project record.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <returns>A service response containing a collection of project modifications.</returns>
    Task<ServiceResponse<IEnumerable<ProjectModificationResponse>>> GetModifications(string projectRecordId);

    /// <summary>
    /// Retrieves all modifications for a given project record filtered by status.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="status">The status to filter modifications by.</param>
    /// <returns>A service response containing a collection of project modifications with the specified status.</returns>
    Task<ServiceResponse<IEnumerable<ProjectModificationResponse>>> GetModificationsByStatus(string projectRecordId, string status);

    /// <summary>
    /// Gets all modifications with filtering, sorting and pagination
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications matching the search criteria.</returns>
    public Task<ServiceResponse<GetModificationsResponse>> GetModifications
   (
       ModificationSearchRequest searchQuery,
       int pageNumber = 1,
       int pageSize = 20,
       string sortField = nameof(ModificationsDto.ModificationId),
       string sortDirection = SortDirections.Descending
   );

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
    public Task<ServiceResponse<GetModificationsResponse>> GetModificationsForProject
   (
       string projectRecordId,
       ModificationSearchRequest searchQuery,
       int pageNumber = 1,
       int pageSize = 20,
       string sortField = nameof(ModificationsDto.ModificationId),
       string sortDirection = SortDirections.Descending
   );

    /// <summary>
    /// Retrieves modifications by a list of modification IDs.
    /// </summary>
    /// <param name="Ids">A list of IDs relating to modifications</param>
    /// <returns>A list of modifications corresponding to the provided IDs</returns>
    Task<ServiceResponse<GetModificationsResponse>> GetModificationsByIds(List<string> Ids);

    /// <summary>
    /// Creates a new project modification.
    /// </summary>
    /// <param name="projectModificationRequest">The request object containing details for the new modification.</param>
    /// <returns>An asynchronous operation that returns the newly created project modification.</returns>
    Task<ServiceResponse<ProjectModificationResponse>> CreateModification(ProjectModificationRequest projectModificationRequest);

    /// <summary>
    /// Creates a new change for an existing project modification.
    /// </summary>
    /// <param name="projectModificationChangeRequest">The request object containing details for the modification change.</param>
    /// <returns>An asynchronous operation that returns the updated project modification change.</returns>
    Task<ServiceResponse<ProjectModificationChangeResponse>> CreateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest);

    /// <summary>
    /// Gets a change for an existing project modification.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier of the project modification change to retrieve.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation, containing a <see cref="ServiceResponse{ProjectModificationChangeResponse}"/>
    /// with the details of the requested project modification change if found; otherwise, an error response.
    /// </returns>
    Task<ServiceResponse<ProjectModificationChangeResponse>> GetModificationChange(Guid modificationChangeId);

    /// <summary>
    /// Gets a change for an existing project modification.
    /// </summary>
    /// <param name="projectModificationChangeRequest">The request object containing details for the modification change.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation, containing a <see cref="ServiceResponse{ProjectModificationChangeResponse}"/>
    /// with the details of the requested project modification change if found; otherwise, an error response.
    /// </returns>
    Task<ServiceResponse> UpdateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest);

    /// <summary>
    /// Retrieves all changes associated with a specific project modification.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record for which to retrieve changes.</param>
    /// <param name="projectModificationId">The unique identifier of the project modification for which to retrieve changes.</param>
    /// <returns>
    /// An asynchronous operation that returns a service response containing a collection of
    /// <see cref="ProjectModificationChangeResponse"/> objects representing the changes for the specified project modification.
    /// </returns>
    Task<ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>> GetModificationChanges(string projectRecordId, Guid projectModificationId);

    ///<summary>
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
    Task<ServiceResponse> CreateDocumentModification(List<ProjectModificationDocumentRequest> projectModificationDocumentRequest);

    Task<ServiceResponse> AssignModificationsToReviewer(List<string> modificationIds, string reviewerId, string reviewerEmail, string reviewerName);

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
    public Task<ServiceResponse<ProjectOverviewDocumentResponse>> GetDocumentsForProjectOverview
   (
       string projectRecordId,
       ProjectOverviewDocumentSearchRequest searchQuery,
       int pageNumber = 1,
       int pageSize = 20,
       string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
       string sortDirection = SortDirections.Descending
   );

    /// <summary>
    /// Removes a change from an existing project modification.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier of the project modification change to remove.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation, containing a <see cref="ServiceResponse{ProjectModificationChangeResponse}"/>
    /// with the details of the requested project modification change if found; otherwise, an error response.
    /// </returns>
    Task<ServiceResponse> RemoveModificationChange(Guid modificationChangeId);

    /// <summary>
    /// Updates the status of an existing project modification by its unique identifier.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record to which the modification belongs.</param>
    /// <param name="modificationId">The unique identifier of the project modification to update.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="ServiceResponse"/>
    /// that reflects the success or failure of the update operation.
    /// </returns>
    Task<ServiceResponse> UpdateModificationStatus(string projectRecordId, Guid modificationId, string status);

    /// <summary>
    /// Updates an existing project modification.
    /// </summary>
    /// <param name="projectModificationRequest">The request containing the details of the project modification to update.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="ServiceResponse"/>
    /// that reflects the success or failure of the update operation.
    /// </returns>
    Task<ServiceResponse> UpdateModification(ProjectModificationRequest projectModificationRequest);

    ///<summary>
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
    Task<ServiceResponse> DeleteDocumentModification(List<ProjectModificationDocumentRequest> projectModificationDocumentRequest);

    /// <summary>
    /// Updates sttaus of an existing project modification by its unique identifier.
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project modification to update.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="ServiceResponse"/>
    /// that reflects the success or failure of the update operation.
    /// </returns>
    Task<ServiceResponse> DeleteModification(string projectRecordId, Guid modificationId);

    Task<ServiceResponse<ProjectModificationAuditTrailResponse>> GetModificationAuditTrail(Guid modificationId);

    /// <summary>
    /// Gets modifications for specific sponsorOrganisationUserId with filtering, sorting and pagination
    /// </summary>
    /// <param name="sponsorOrganisationUserId">The unique identifier of the sponsor organisation user for which modifications are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications related to the specified project record.</returns>
    public Task<ServiceResponse<GetModificationsResponse>> GetModificationsBySponsorOrganisationUserId
   (
       Guid sponsorOrganisationUserId,
       SponsorAuthorisationsModificationsSearchRequest searchQuery,
       int pageNumber = 1,
       int pageSize = 20,
       string sortField = nameof(ModificationsDto.SentToSponsorDate),
       string sortDirection = SortDirections.Descending
   );

    /// <summary>
    /// Saves review responses for a project modification.
    /// </summary>
    /// <param name="modificationReviewRequest">The request object containing the review values</param>
    public Task<ServiceResponse> SaveModificationReviewResponses(ProjectModificationReviewRequest modificationReviewRequest);

    /// <summary>
    /// Gets review responses for a project modification.
    /// </summary>
    /// <param name="projectRecordId">The ID of the project record</param>
    /// <param name="modificationId">The ID of the modification</param>
    /// <returns>Returns the modification review properties</returns>
    public Task<ServiceResponse<ProjectModificationReviewResponse>> GetModificationReviewResponses(string projectRecordId, Guid modificationId);

    /// <summary>
    /// Gets modifications for specific modificationId with filtering, sorting and pagination
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project record for which modifications are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications related to the specified project record.</returns>
    public Task<ServiceResponse<ProjectOverviewDocumentResponse>> GetDocumentsForModification
   (
       Guid modificationId,
       ProjectOverviewDocumentSearchRequest searchQuery,
       int pageNumber = 1,
       int pageSize = 20,
       string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
       string sortDirection = SortDirections.Descending
   );

    /// <summary>
    /// Updates an existing project modification change.
    /// </summary>
    /// <param name="modificationId">The request object containing the updated details for the modification change.</param>
    public Task<ServiceResponse> CheckDocumentAccess(Guid modificationId);

    /// <summary>
    /// Saves review responses for a project modification.
    /// </summary>
    /// <param name="documentsAuditTrailRequest">The request object containing the review values</param>
    public Task<ServiceResponse> CreateModificationDocumentsAuditTrail(List<ModificationDocumentsAuditTrailDto> documentsAuditTrailRequest);
}