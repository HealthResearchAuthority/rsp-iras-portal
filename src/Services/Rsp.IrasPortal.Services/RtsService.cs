using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

/// <summary>
/// Service class to interact with the RTS microservice through the IRtsServiceClient.
/// Implements the IRtsService interface.
/// </summary>
public class RtsService(IRtsServiceClient rtsServiceClient) : IRtsService
{
    /// <summary>
    /// Retrieves an organisation by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the organisation.</param>
    /// <returns>A service response containing the organisation details.</returns>
    public async Task<ServiceResponse<OrganisationDto>> GetOrganisation(string id)
    {
        var apiResponse = await rtsServiceClient.GetOrganisation(id);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Retrieves a list of organisations filtered by name and optionally by role.
    /// </summary>
    /// <param name="name">The name of the organisation to search for.</param>
    /// <param name="role">The optional role of the organisation to filter by.</param>
    /// <returns>A service response containing a list of organisations.</returns>
    public async Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisations(string name, string? role)
    {
        var apiResponse = await rtsServiceClient.GetOrganisations(name, role);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Retrieves a paginated list of organisations filtered by name and optionally by role.
    /// </summary>
    /// <param name="name">The name of the organisation to search for.</param>
    /// <param name="role">The optional role of the organisation to filter by.</param>
    /// <param name="pageSize">The number of organisations to retrieve per page.</param>
    /// <returns>A service response containing a paginated list of organisations.</returns>
    public async Task<ServiceResponse<OrganisationSearchResponse>> GetOrganisations(string name, string? role, int pageSize)
    {
        var apiResponse = await rtsServiceClient.GetOrganisations(name, role, pageSize);

        return apiResponse.ToServiceResponse();
    }
}