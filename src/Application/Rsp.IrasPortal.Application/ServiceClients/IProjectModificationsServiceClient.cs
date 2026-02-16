using Refit;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Application.ServiceClients;

/// <summary>
/// Interface to interact with the Iras microservice.
/// Provides methods to retrieve, create, and manage project modification records.
/// </summary>
public interface IProjectModificationsServiceClient
{
    /// <summary>
    /// Gets the saved project modification by project record Id and modification Id.
    /// </summary>
    /// <param name="projectModificationId">The unique identifier of the project modification.</param>
    /// <returns>An asynchronous operation that returns the saved project modification.</returns>
    [Get("/projectmodifications/{projectRecordId}/{projectModificationId}")]
    public Task<ApiResponse<ProjectModificationResponse>> GetModification(string projectRecordId, Guid projectModificationId);

    /// <summary>
    /// Gets all saved project modifications for a given project record Id.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <returns>An asynchronous operation that returns all saved project modifications for the specified project record.</returns>
    [Get("/projectmodifications")]
    public Task<ApiResponse<IEnumerable<ProjectModificationResponse>>> GetModifications(string projectRecordId);

    /// <summary>
    /// Gets all saved project modifications for a given project record Id and status.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="status">The status of the project modification.</param>
    /// <returns>An asynchronous operation that returns all saved project modifications with the specified status.</returns>
    [Get("/projectmodifications/{projectRecordId}/{status}")]
    public Task<ApiResponse<IEnumerable<ProjectModificationResponse>>> GetModificationsByStatus(string projectRecordId, string status);

    /// <summary>
    /// Gets all modifications with filtering, sorting and pagination
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications matching the search criteria.</returns>
    [Post("/projectmodifications/getallmodifications")]
    public Task<ApiResponse<GetModificationsResponse>> GetModifications
    (
        [Body] ModificationSearchRequest searchQuery,
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
    [Post("/projectmodifications/getmodificationsforproject")]
    public Task<ApiResponse<GetModificationsResponse>> GetModificationsForProject
    (
        string projectRecordId,
        [Body] ModificationSearchRequest searchQuery,
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
    [Post("/projectmodifications/getmodificationsbyids")]
    public Task<ApiResponse<GetModificationsResponse>> GetModificationsByIds(List<string> Ids);

    /// <summary>
    /// Creates a new project modification.
    /// </summary>
    /// <param name="projectModificationRequest">The request object containing details for the new project modification.</param>
    /// <returns>An asynchronous operation that returns the newly created project modification.</returns>
    [Post("/projectmodifications")]
    public Task<ApiResponse<ProjectModificationResponse>> CreateModification(ProjectModificationRequest projectModificationRequest);

    /// <summary>
    /// Creates a new project modification change.
    /// </summary>
    /// <param name="projectModificationChangeRequest">The request object containing details for the project modification change.</param>
    /// <returns>An asynchronous operation that returns the newly created project modification change.</returns>
    [Post("/projectmodifications/change")]
    public Task<ApiResponse<ProjectModificationChangeResponse>> CreateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest);

    /// <summary>
    /// Retrieves a project modification change by its unique identifier.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier of the project modification change.</param>
    /// <returns>An asynchronous operation that returns the requested project modification change.</returns>
    [Get("/projectmodifications/change")]
    public Task<ApiResponse<ProjectModificationChangeResponse>> GetModificationChange(Guid modificationChangeId);

    /// <summary>
    /// Retrieves all modification changes for a modification.
    /// </summary>
    /// <param name="projectModificationId">The unique identifier of the project modification.</param>
    /// <returns>An asynchronous operation that returns the requested project modification changes.</returns>
    [Get("/projectmodifications/changes")]
    public Task<ApiResponse<IEnumerable<ProjectModificationChangeResponse>>> GetModificationChanges(string projectRecordId, Guid projectModificationId);

    /// <summary>
    /// Creates one or more modification documents associated with a project modification change.
    /// </summary>
    /// <param name="projectModificationChangeRequest">
    /// A list of <see cref="ProjectModificationDocumentRequest"/> representing the documents to be created.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the API response indicating success or failure.
    /// </returns>
    [Post("/projectmodifications/createdocument")]
    public Task<IApiResponse> CreateModificationDocument(List<ProjectModificationDocumentRequest> projectModificationChangeRequest);

    /// <summary>
    /// Assigns a list of modifications to a study-wide reviewer.
    /// </summary>
    /// <param name="modificationIds">A list of modification IDs</param>
    /// <param name="reviewerId">The user ID of the study-wide reviewer</param>
    /// <returns></returns>
    [Post("/projectmodifications/assignmodificationstoreviewer")]
    public Task<IApiResponse> AssignModificationsToReviewer(List<string> modificationIds, string reviewerId, string reviewerEmail, string reviewerName);

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
    [Post("/projectmodifications/getdocumentsforprojectoverview")]
    public Task<ApiResponse<ProjectOverviewDocumentResponse>> GetDocumentsForProjectOverview
    (
        string projectRecordId,
        [Body] ProjectOverviewDocumentSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Descending
    );

    /// <summary>
    /// Removes a project modification change by its unique identifier.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier of the project modification change.</param>
    /// <returns>An asynchronous operation that returns the requested project modification change.</returns>
    [Delete("/projectmodifications/remove")]
    public Task<IApiResponse> RemoveModificationChange(Guid modificationChangeId);

    /// <summary>
    /// Updates a project modification status by its unique identifier.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="modificationId">The unique identifier of the project modification.</param>
    /// <returns>An asynchronous operation that returns the requested project modification change.</returns>
    [Patch("/projectmodifications/status")]
    public Task<IApiResponse> UpdateModificationStatus(string projectRecordId, Guid modificationId, string status, string? revisionDescription = null, string? reasonNotApproved = null);

    /// <summary>
    /// Deletes one or more modification documents associated with a project modification change.
    /// </summary>
    /// <param name="projectModificationChangeRequest">
    /// A list of <see cref="ProjectModificationDocumentRequest"/> representing the documents to be created.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the API response indicating success or failure.
    /// </returns>
    [Post("/projectmodifications/deletedocuments")]
    public Task<IApiResponse> DeleteDocuments(List<ProjectModificationDocumentRequest> projectModificationChangeRequest);

    /// <summary>
    /// Deletes a project modification by its unique identifier.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="modificationId">The unique identifier of the project modification.</param>
    /// <returns>An asynchronous operation that returns the requested project modification change.</returns>
    [Post("/projectmodifications/delete")]
    public Task<IApiResponse> DeleteModification(string projectRecordId, Guid modificationId);

    /// <summary>
    /// Gets the audit trail for a specific project modification.
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project modification</param>
    /// <returns>A list of modification audit trail records and total record count</returns>
    [Get("/projectmodifications/audittrail")]
    public Task<ApiResponse<ProjectModificationAuditTrailResponse>> GetModificationAuditTrail(Guid modificationId);

    /// <summary>
    /// Gets modifications for specific SponsorOrganisationUserId with filtering, sorting and pagination
    /// </summary>
    /// <param name="sponsorOrganisationUserId">The unique identifier of the sponsor organisation user for which modifications are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications related to the specified project record.</returns>
    [Post("/projectmodifications/getmodificationsbysponsororganisationuserid")]
    public Task<ApiResponse<GetModificationsResponse>> GetModificationsBySponsorOrganisationUserId
    (
        Guid sponsorOrganisationUserId,
        [Body] SponsorAuthorisationsModificationsSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsDto.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    );

    /// <summary>
    /// Saves review responses for a project modification.
    /// </summary>
    /// <param name="modificationReviewRequest">The request object containing the review values</param>
    [Post("/projectmodifications/savereviewresponses")]
    public Task<IApiResponse> SaveModificationReviewResponses(ProjectModificationReviewRequest modificationReviewRequest);

    /// <summary>
    /// Gets review responses for a project modification.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="modificationId">The ID of the modification</param>
    /// <returns>Returns the modification review properties</returns>
    [Get("/projectmodifications/getreviewresponses")]
    public Task<ApiResponse<ProjectModificationReviewResponse>> GetModificationReviewResponses(string projectRecordId, Guid modificationId);

    /// <summary>
    /// Gets modifications for specific ProjectRecordId with filtering, sorting and pagination
    /// </summary>
    /// <param name="modificationId">The unique identifier of the modification for which documents are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for modifications.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of modifications related to the specified project record.</returns>
    [Post("/projectmodifications/getdocumentsformodification")]
    public Task<ApiResponse<ProjectOverviewDocumentResponse>> GetDocumentsForModification
    (
        Guid modificationId,
        [Body] ProjectOverviewDocumentSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Descending
    );

    /// <summary>
    /// Updates an existing project modification.
    /// </summary>
    /// <param name="projectModificationRequest">The request object containing the updated details for the project modification.</param>
    [Patch("/projectmodifications")]
    Task<IApiResponse> UpdateModification(ProjectModificationRequest projectModificationRequest);

    /// <summary>
    /// Updates an existing project modification change.
    /// </summary>
    /// <param name="projectModificationChangeRequest">The request object containing the updated details for the modification change.</param>
    [Patch("/projectmodifications/change")]
    Task<IApiResponse> UpdateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest);

    /// <summary>
    /// Updates an existing project modification change.
    /// </summary>
    /// <param name="modificationId">The request object containing the updated details for the modification change.</param>
    [Get("/documents/access/{modificationId}")]
    Task<IApiResponse> CheckDocumentAccess(Guid modificationId);

    /// <summary>
    /// Updates an existing project modification change.
    /// </summary>
    /// <param name="documentsAuditTrailRequest">The request object containing the updated details for the modification change.</param>
    [Post("/documents/createdocumentsaudittrail")]
    Task<IApiResponse> CreateModificationDocumentsAuditTrail([Body] List<ModificationDocumentsAuditTrailDto> documentsAuditTrailRequest);
}