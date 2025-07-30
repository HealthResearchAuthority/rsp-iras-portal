using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with RTS microservice.
/// </summary>
public interface IRtsServiceClient
{
    /// <summary>
    /// Gets all organisations, with optional role filtering and paging.
    /// </summary>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    [Get("/organisations/getall")]
    public Task<ApiResponse<OrganisationSearchResponse>> GetOrganisations(string? role, int pageIndex = 1, int? pageSize = null);

    /// <summary>
    /// Searches for organisations by name, with optional role filtering and paging.
    /// </summary>
    /// <param name="name">The name or partial name of the organisation to search for.</param>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    [Get("/organisations/searchbyname")]
    public Task<ApiResponse<OrganisationSearchResponse>> GetOrganisationsByName(string name, string? role, int pageIndex = 1, int? pageSize = null);

    /// <summary>
    /// Gets the organisation by Id
    /// </summary>
    /// <param name="id">Organisation Id</param>
    /// <returns>An asynchronous operation that returns an organisation.</returns>
    [Get("/organisations/getbyid")]
    public Task<ApiResponse<OrganisationDto>> GetOrganisation(string id);
}