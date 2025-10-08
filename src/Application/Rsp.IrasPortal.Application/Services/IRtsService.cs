using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Rts Service Interface. Marked as IInterceptable to enable
/// the start/end logging for all methods.
/// </summary>
public interface IRtsService : IInterceptable
{
    /// <summary>
    /// Gets the organisations by name and role with optional pagination, country filtering, and sorting.
    /// </summary>
    /// <param name="name">The name or partial name of the organisation to search for.</param>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <param name="countries">Optional list of CountryName values to filter by (e.g., "England", "Wales").</param>
    /// <param name="sort">Sort direction: "asc" or "desc". Defaults to "asc".</param>
    /// <param name="sortField">Sort field: "name", "country", or "isactive". Defaults to "name".</param>
    Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisationsByName(
        string name,
        string? role,
        int pageIndex = 1,
        int? pageSize = null,
        IEnumerable<string>? countries = null,
        string sort = "asc",
        string sortField = "name");

    /// <summary>
    /// Gets all organisations, with optional role/country filtering, sorting, and paging.
    /// </summary>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <param name="countries">Optional list of CountryName values to filter by (e.g., "England", "Wales").</param>
    /// <param name="sort">Sort direction: "asc" or "desc". Defaults to "asc".</param>
    /// <param name="sortField">Sort field: "name", "country", or "isactive". Defaults to "name".</param>
    Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisations(
        string? role,
        int pageIndex = 1,
        int? pageSize = null,
        IEnumerable<string>? countries = null,
        string sort = "asc",
        string sortField = "name");

    /// <summary>
    /// Gets the organisation by Id
    /// </summary>
    /// <param name="id">Organisation Id</param>
    /// <returns>An asynchronous operation that returns an organisation.</returns>
    public Task<ServiceResponse<OrganisationDto>> GetOrganisation(string id);
}