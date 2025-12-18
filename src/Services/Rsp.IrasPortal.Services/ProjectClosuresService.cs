using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class ProjectClosuresService
(
    IProjectClosuresServiceClient projectClosuresServiceClient
) : IProjectClosuresService
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
    public async Task<ServiceResponse<ProjectClosuresResponse>> GetProjectClosuresBySponsorOrganisationUserId
    (
        Guid sponsorOrganisationUserId,
        ProjectClosuresSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectClosuresDto.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    )
    {
        var apiResponse = await projectClosuresServiceClient.GetProjectClosuresBySponsorOrganisationUserId(sponsorOrganisationUserId, searchQuery, pageNumber, pageSize, sortField, sortDirection);
        return apiResponse.ToServiceResponse();
    }
}