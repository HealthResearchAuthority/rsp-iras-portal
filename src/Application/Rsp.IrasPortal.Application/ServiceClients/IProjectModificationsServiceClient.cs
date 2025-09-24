using Refit;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with the Iras microservice.
/// Provides methods to retrieve, create, and manage project modification records.
/// </summary>
public interface IProjectModificationsServiceClient
{
    /// <summary>
    /// Gets the saved project modification by project record Id and modification Id.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="projectModificationId">The unique identifier of the project modification.</param>
    /// <returns>An asynchronous operation that returns the saved project modification.</returns>
    [Get("/projectmodifications/{projectRecordId}")]
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
    /// Gets all the area of changes and specific area of changes for the modification.
    /// </summary>
    /// <returns>An asynchronous operation that returns all area of changes and specific area of changes.</returns>
    [Get("/projectmodifications/areaofchanges")]
    public Task<ApiResponse<IEnumerable<GetAreaOfChangesResponse>>> GetAreaOfChanges();

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
    public Task<IApiResponse> AssignModificationsToReviewer(List<string> modificationIds, string reviewerId);

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
}