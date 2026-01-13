using Refit;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Application.ServiceClients;

/// <summary>
/// Interface to interact with RTS microservice.
/// </summary>

public interface IRtsServiceClient
{
    /// <summary>
    /// Gets all organisations, with optional role/country filtering, sorting and paging.
    /// </summary>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <param name="countries">Optional list of CountryName values. Sent as ?countries=England&countries=Wales.</param>
    /// <param name="sort">Sort direction: "asc" or "desc". Defaults to "asc".</param>
    /// <param name="sortField">Sort field: "name", "country", or "isactive". Defaults to "name".</param>
    [Get("/organisations/getAll")]
    Task<ApiResponse<OrganisationSearchResponse>> GetOrganisations(
        string? role,
        int pageIndex = 1,
        int? pageSize = null,
        [Query(CollectionFormat.Multi), AliasAs("countries")] IEnumerable<string>? countries = null,
        string sort = "asc",
        string sortField = "name");

    /// <summary>
    /// Searches for organisations by name, with optional role/country filtering, sorting and paging.
    /// </summary>
    /// <param name="name">The name or partial name of the organisation to search for.</param>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <param name="countries">Optional list of CountryName values. Sent as ?countries=England&countries=Wales.</param>
    /// <param name="sort">Sort direction: "asc" or "desc". Defaults to "asc".</param>
    /// <param name="sortField">Sort field: "name", "country", or "isactive". Defaults to "name".</param>
    [Get("/organisations/searchByName")]
    Task<ApiResponse<OrganisationSearchResponse>> GetOrganisationsByName(
        string name,
        string? role,
        int pageIndex = 1,
        int? pageSize = null,
        [Query(CollectionFormat.Multi), AliasAs("countries")] IEnumerable<string>? countries = null,
        string sort = "asc",
        string sortField = "name");



    /// <summary>
    /// Gets the organisation by Id
    /// </summary>
    /// <param name="id">Organisation Id</param>
    /// <returns>An asynchronous operation that returns an organisation.</returns>
    [Get("/organisations/getbyid")]
    public Task<ApiResponse<OrganisationDto>> GetOrganisation(string id);
}

