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
    /// Gets the organisations by name and role
    /// </summary>
    /// <param name="name">Organisation name</param>
    /// <param name="role">Role of the Organisation</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    public Task<ServiceResponse<IEnumerable<OrganisationSearchResponse>>> GetOrganisations(string name, string? role);

    /// <summary>
    /// Gets the specified number of organisations by name and role
    /// </summary>
    /// <param name="name">Organisation name</param>
    /// <param name="role">Role of the Organisation</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    public Task<ServiceResponse<IEnumerable<OrganisationSearchResponse>>> GetOrganisations(string name, string? role, int pageSize);

    /// <summary>
    /// Gets the organisation by Id
    /// </summary>
    /// <param name="id">Organisation Id</param>
    /// <returns>An asynchronous operation that returns an organisation.</returns>
    public Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisation(string id);
}