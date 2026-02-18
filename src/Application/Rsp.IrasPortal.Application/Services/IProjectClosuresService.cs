using Rsp.Logging.Interceptors;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;

namespace Rsp.Portal.Application.Services;

public interface IProjectClosuresService : IInterceptable
{
    /// <summary>
    /// Gets project closures records for specific sponsorOrganisationUserId with filtering, sorting
    /// and pagination
    /// </summary>
    /// <param name="sponsorOrganisationUserId">
    /// The unique identifier of the sponsor organisation user for which project closures are requested.
    /// </param>
    /// <param name="searchQuery">Object containing filtering criteria for project closures.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">
    /// The direction of sorting: "asc" for ascending or "desc" for descending.
    /// </param>
    /// <returns>Returns a paginated list of project closures.</returns>
    public Task<ServiceResponse<ProjectClosuresSearchResponse>> GetProjectClosuresBySponsorOrganisationUserId
   (
       Guid sponsorOrganisationUserId,
       ProjectClosuresSearchRequest searchQuery,
       string rtsId,
       int pageNumber = 1,
       int pageSize = 20,
       string sortField = nameof(ProjectClosuresDto.SentToSponsorDate),
       string sortDirection = SortDirections.Descending

   );

    /// <summary>
    /// Create project closure record in project closure table
    /// </summary>
    /// <param name="projectClosureRequest"></param>
    /// <returns>Returns the newly inserted project closure record</returns>
    public Task<ServiceResponse<ProjectClosuresResponse>> CreateProjectClosure(ProjectClosureRequest projectClosureRequest);

    /// <summary>
    /// Updates the status of an existing project closure by its project unique identifier.
    /// </summary>
    /// <param name="projectRecordId">
    /// The unique identifier of the project record to which the project closure belongs.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="ServiceResponse"/>
    /// that reflects the success or failure of the update operation.
    /// </returns>
    public Task<ServiceResponse> UpdateProjectClosureStatus(string projectRecordId, string status);

    /// <summary>
    /// Gets a project closure records from project closure table based on projectRecordId
    /// </summary>
    /// <param name="projectRecordId"></param>
    /// <returns>Returns the project closure record</returns>
    public Task<ServiceResponse<ProjectClosuresSearchResponse>> GetProjectClosuresByProjectRecordId(string projectRecordId);

    /// <summary>
    /// Gets project closures records for specific sponsorOrganisationUserId with filtering, but
    /// without pagination
    /// </summary>
    /// <param name="sponsorOrganisationUserId">
    /// The unique identifier of the sponsor organisation user for which project closures are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for project closures.</param>
    /// <returns>Returns a collection of project closures.</returns>
    public Task<ServiceResponse<ProjectClosuresSearchResponse>> GetProjectClosuresBySponsorOrganisationUserIdWithoutPaging
   (
       Guid sponsorOrganisationUserId,
       ProjectClosuresSearchRequest searchQuery,
       string rtsId
   );
}