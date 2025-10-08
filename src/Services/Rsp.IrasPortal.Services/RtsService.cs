using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

/// <summary>
///     Service class to interact with the RTS microservice through the IRtsServiceClient.
///     Implements the IRtsService interface.
/// </summary>
public class RtsService(IRtsServiceClient rtsServiceClient) : IRtsService
{
    /// <summary>
    ///     Retrieves an organisation by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the organisation.</param>
    /// <returns>A service response containing the organisation details.</returns>
    public async Task<ServiceResponse<OrganisationDto>> GetOrganisation(string id)
    {
        var apiResponse = await rtsServiceClient.GetOrganisation(id);

        return apiResponse.ToServiceResponse();
    }


    /// <summary>
    ///     Gets all organisations, with optional role/country filtering, sorting, and paging.
    /// </summary>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <param name="countries">Optional list of CountryName values to filter by (e.g., "England", "Wales").</param>
    /// <param name="sort">Sort direction: "asc" or "desc". Defaults to "asc".</param>
    /// <param name="sortField">Sort field: "name", "country", or "isactive". Defaults to "name".</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    public async Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisations(
        string? role,
        int pageIndex = 1,
        int? pageSize = null,
        IEnumerable<string>? countries = null,
        string sort = "asc",
        string sortField = "name")
    {
        var apiResponse = await rtsServiceClient.GetOrganisations(
            role,
            pageIndex,
            pageSize,
            countries,
            sort,
            sortField);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisationsByName(string name, string? role,
        int pageIndex = 1,
        int? pageSize = null,
        IEnumerable<string>? countries = null,
        string sort = "asc",
        string sortField = "name")
    {
        var apiResponse = await rtsServiceClient.GetOrganisationsByName(
            name.ToLower(),
            role,
            pageIndex,
            pageSize,
            countries,
            sort,
            sortField);

        return apiResponse.ToServiceResponse();
    }
}