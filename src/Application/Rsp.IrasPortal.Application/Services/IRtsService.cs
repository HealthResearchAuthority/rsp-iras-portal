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
    /// Gets the organisations by name and role with optional pagination
    /// </summary>
    /// <param name="name">The name or partial name of the organisation to search for.</param>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    public Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisationsByName(string name, string? role, int pageIndex = 1, int? pageSize = null);

    /// <summary>
    /// Gets all organisations, with optional role filtering and paging.
    /// </summary>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">Index (1-based) of page for paginated results.</param>
    /// <param name="pageSize">Optional maximum number of results to return.</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    public Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisations(string? role, int pageIndex = 1, int? pageSize = null);

    /// <summary>
    /// Gets the organisation by Id
    /// </summary>
    /// <param name="id">Organisation Id</param>
    /// <returns>An asynchronous operation that returns an organisation.</returns>
    public Task<ServiceResponse<OrganisationDto>> GetOrganisation(string id);
}