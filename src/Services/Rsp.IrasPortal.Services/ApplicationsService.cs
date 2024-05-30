using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Services;

public class ApplicationsService(ILogger<ApplicationsService> logger, IApplicationsServiceClient applicationsClient) : IApplicationsService
{
    /// <inheritdoc/>
    public Task<IrasApplication> GetApplication(int id)
    {
        return applicationsClient.GetApplication(id);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IrasApplication>> GetApplications()
    {
        return applicationsClient.GetApplications();
    }

    /// <inheritdoc/>
    public Task<ServiceResponse<IrasApplication>> GetApplicationByStatus(int id, string status)
    {
        return applicationsClient.GetApplicationByStatus(id, status);
    }

    /// <inheritdoc/>
    public Task<ServiceResponse<IEnumerable<IrasApplication>>> GetApplicationsByStatus(string status)
    {
        return applicationsClient.GetApplicationsByStatus(status);
    }

    /// <inheritdoc/>
    public Task<IrasApplication> CreateApplication(IrasApplication irasApplication)
    {
        return applicationsClient.CreateApplication(irasApplication);
    }

    /// <inheritdoc/>
    public Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication)
    {
        return applicationsClient.UpdateApplication(id, irasApplication);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> AddApplicationCategory(string categoryName)
    {
        return applicationsClient.AddApplicationCategory(categoryName);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> AddProjectCategory(string categoryName)
    {
        return applicationsClient.AddProjectCategory(categoryName);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetApplicationCategories()
    {
        logger.LogMethodStarted(LogLevel.Information);

        return applicationsClient.GetApplicationCategories();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetProjectCategories()
    {
        logger.LogMethodStarted(LogLevel.Information);

        return applicationsClient.GetProjectCategories();
    }
}