using Refit;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface IRtsServiceClient
{
    /// <summary>
    ///     Searches for an organisation by its name
    /// </summary>
    [Get("/organisations/searchbyname")]
    public Task<ApiResponse<IEnumerable<RtsSearchByNameDto>>> SearchOrganisationByName([Query] string name);
}