using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Service interface for managing project modifications.
/// Marked as IInterceptable to enable start/end logging for all methods.
/// </summary>
public interface IProjectModificationsService : IInterceptable
{
    /// <summary>
    /// Retrieves a specific project modification by project record ID and modification ID.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
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
    /// Gets all the area of changes and specific area of changes for the modification.
    /// </summary>
    Task<ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>> GetAreaOfChanges();

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
    Task<ServiceResponse> CreateDocumentModification(List<ProjectModificationDocumentRequest> projectModificationDocumentRequest);
}