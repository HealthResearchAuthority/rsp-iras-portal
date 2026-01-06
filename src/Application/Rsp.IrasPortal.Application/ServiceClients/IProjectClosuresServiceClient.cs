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
    public Task<ApiResponse<ProjectClosuresResponse>> GetProjectClosuresBySponsorOrganisationUserId
    (
        Guid sponsorOrganisationUserId,
        [Body] ProjectClosuresSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectClosuresDto.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    );

    [Post("/projectclosure/createprojectclosure")]
    public Task<ApiResponse<ProjectClosuresResponse>> CreateProjectClosure(ProjectClosureRequest projectClosureRequest);
}