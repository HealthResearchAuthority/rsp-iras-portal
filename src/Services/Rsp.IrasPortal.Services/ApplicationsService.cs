using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class ApplicationsService(IApplicationsServiceClient applicationsClient) : IApplicationsService
{
    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> GetApplication(string applicationId)
    {
        var apiResponse = await applicationsClient.GetApplication(applicationId);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplications()
    {
        var apiResponse = await applicationsClient.GetApplications();

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> GetApplicationByStatus(string applicationId, string status)
    {
        var apiResponse = await applicationsClient.GetApplicationByStatus(applicationId, status);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByStatus(string status)
    {
        var apiResponse = await applicationsClient.GetApplicationsByStatus(status);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByRespondent(string respondentId)
    {
        var apiResponse = await applicationsClient.GetApplicationsByRespondent(respondentId);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication)
    {
        var apiResponse = await applicationsClient.CreateApplication(irasApplication);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication)
    {
        var apiResponse = await applicationsClient.UpdateApplication(irasApplication);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<GetModificationsResponse>> GetModifications(ModificationSearchRequest searchQuery, int pageNumber = 1, int pageSize = 20)
    {
        var apiResponse = await applicationsClient.GetModifications(searchQuery, pageNumber, pageSize);

        return apiResponse.ToServiceResponse();
    }
}