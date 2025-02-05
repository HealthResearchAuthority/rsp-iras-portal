using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

/// <inheritdoc />
public class RtsService(IRtsServiceClient client) : IRtsService
{
    public async Task<ServiceResponse<IEnumerable<RtsSearchByNameDto>>> SearchByName(string searchTerm)
    {
        var apiResponse = await client.SearchOrganisationByName(searchTerm);

        return apiResponse.ToServiceResponse();
    }
}