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
public interface IProjectClosuresServiceClient
{
    /// <summary>
    /// Gets project closures records for specific sponsorOrganisationUserId with filtering, sorting and pagination
    /// </summary>
    /// <param name="sponsorOrganisationUserId">The unique identifier of the sponsor organisation user for which project closures are requested.</param>
    /// <param name="searchQuery">Object containing filtering criteria for project closures.</param>
    /// <param name="pageNumber">The number of the page to retrieve (used for pagination - 1-based).</param>
    /// <param name="pageSize">The number of items per page (used for pagination).</param>
    /// <param name="sortField">The field name by which the results should be sorted.</param>
    /// <param name="sortDirection">The direction of sorting: "asc" for ascending or "desc" for descending.</param>
    /// <returns>Returns a paginated list of project closures.</returns>
    [Post("/projectclosure/getprojectclosuresbysponsororganisationuserid")]
    public Task<ApiResponse<ProjectClosuresSearchResponse>> GetProjectClosuresBySponsorOrganisationUserId
    (
        Guid sponsorOrganisationUserId,
        [Body] ProjectClosuresSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectClosuresDto.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    );

    /// <summary>
    /// Create a record in the project closure table
    /// </summary>
    /// <param name="projectClosureRequest"></param>
    /// <returns></returns>
    [Post("/projectclosure/createprojectclosure")]
    public Task<ApiResponse<ProjectClosuresResponse>> CreateProjectClosure(ProjectClosureRequest projectClosureRequest);

    /// <summary>
    /// Get the record from the project closure table
    /// </summary>
    /// <param name="projectRecordId"></param>
    /// <returns></returns>
    [Get("/projectclosure/getprojectclosurebyid")]
    public Task<ApiResponse<ProjectClosuresResponse>> GetProjectClosureById(string projectRecordId);

    /// <summary>
    /// Updates the project closure status to either Authorised or Not authorised.
    /// If the status is set to Authorised, the method also closes the associated project record
    /// by updating its status to Closed.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record whose closure status will be updated.</param>
    /// <param name="status">The new closure status to apply (Authorised or Not authorised).</param>
    /// <returns>An API response indicating the result of the operation.</returns>
    [Patch("/projectclosure/updateprojectclosurestatus")]
    public Task<IApiResponse> UpdateProjectClosureStatus(string projectRecordId, string status);
}