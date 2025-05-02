using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with RTS microservice.
/// </summary>
public interface IRtsServiceClient
{
    /// <summary>
    /// Gets the organisations by name and role
    /// </summary>
    /// <param name="name">Organisation name</param>
    /// <param name="role">Role of the Organisation</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    [Get("/organisations/searchbyname")]
    public Task<ApiResponse<IEnumerable<OrganisationSearchResponse>>> GetOrganisations(string name, string? role);

    /// <summary>
    /// Gets the specified number of organisations by name and role
    /// </summary>
    /// <param name="name">Organisation name</param>
    /// <param name="role">Role of the Organisation</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>An asynchronous operation that returns organisations.</returns>
    [Get("/organisations/searchbyname")]
    public Task<ApiResponse<IEnumerable<OrganisationSearchResponse>>> GetOrganisations(string name, string? role, int pageSize);

    /// <summary>
    /// Gets the organisation by Id
    /// </summary>
    /// <param name="id">Organisation Id</param>
    /// <returns>An asynchronous operation that returns an organisation.</returns>
    [Get("/organisations/getbyid")]
    public Task<ApiResponse<OrganisationSearchResponse>> GetOrganisation(string id);
}