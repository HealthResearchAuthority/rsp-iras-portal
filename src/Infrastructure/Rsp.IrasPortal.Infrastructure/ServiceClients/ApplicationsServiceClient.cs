using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Infrastructure.HttpClients;

namespace Rsp.IrasPortal.Infrastructure.ServiceClients;

public class ApplicationsServiceClient(IApplicationsHttpClient client) : IApplicationsServiceClient
{
    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> GetApplication(string applicationId)
    {
        var apiResponse = await client.GetApplication(applicationId);

        return GetServiceResponse(apiResponse);
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplications()
    {
        var apiResponse = await client.GetApplications();

        return GetServiceResponse(apiResponse);
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> GetApplicationByStatus(string applicationId, string status)
    {
        var apiResponse = await client.GetApplicationByStatus(applicationId, status);

        return GetServiceResponse(apiResponse);
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByStatus(string status)
    {
        var apiResponse = await client.GetApplicationsByStatus(status);

        return GetServiceResponse(apiResponse);
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication)
    {
        var apiResponse = await client.CreateApplication(irasApplication);

        return GetServiceResponse(apiResponse);
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication)
    {
        var apiResponse = await client.UpdateApplication(irasApplication);

        return GetServiceResponse(apiResponse);
    }

    private static ServiceResponse<T> GetServiceResponse<T>(ApiResponse<T> apiResponse)
    {
        var serviceResponse = new ServiceResponse<T>();

        return apiResponse.IsSuccessStatusCode switch
        {
            true =>
                serviceResponse
                    .WithStatus()
                    .WithContent(apiResponse.Content),

            _ => serviceResponse
                    .WithError
            (
                        apiResponse.Error?.Message,
                        apiResponse.Error?.ReasonPhrase,
                        apiResponse.StatusCode
                    )
        };
    }
}