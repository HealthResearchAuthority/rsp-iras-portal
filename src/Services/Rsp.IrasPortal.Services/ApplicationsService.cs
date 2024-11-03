using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Services;

public class ApplicationsService(ILogger<ApplicationsService> logger, IApplicationsServiceClient applicationsClient) : IApplicationsService
{
    /// <inheritdoc/>
    public Task<ServiceResponse<IrasApplicationResponse>> GetApplication(string applicationId)
    {
        return applicationsClient.GetApplication(applicationId);
    }

    /// <inheritdoc/>
    public Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplications()
    {
        return applicationsClient.GetApplications();
    }

    /// <inheritdoc/>
    public Task<ServiceResponse<IrasApplicationResponse>> GetApplicationByStatus(string applicationId, string status)
    {
        return applicationsClient.GetApplicationByStatus(applicationId, status);
    }

    /// <inheritdoc/>
    public Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByStatus(string status)
    {
        return applicationsClient.GetApplicationsByStatus(status);
    }

    /// <inheritdoc/>
    public Task<ServiceResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication)
    {
        return applicationsClient.CreateApplication(irasApplication);
    }

    /// <inheritdoc/>
    public Task<ServiceResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication)
    {
        return applicationsClient.UpdateApplication(irasApplication);
    }
}