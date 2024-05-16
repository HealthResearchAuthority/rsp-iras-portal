using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Infrastructure.HttpClients;

namespace Rsp.IrasPortal.Infrastructure.ServiceClients;

public class ApplicationsServiceClient(IApplicationsHttpClient client) : IApplicationsServiceClient
{
    /// <inheritdoc/>
    public async Task<IrasApplication> GetApplication(int id)
    {
        return await client.GetApplication(id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IrasApplication>> GetApplications()
    {
        return await client.GetApplications();
    }

    /// <inheritdoc/>
    public async Task<IrasApplication> CreateApplication(IrasApplication irasApplication)
    {
        return await client.CreateApplication(irasApplication);
    }

    /// <inheritdoc/>
    public async Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication)
    {
        return await client.UpdateApplication(id, irasApplication);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> AddApplicationCategory(string categoryName)
    {
        await client.AddApplicationCategory(categoryName);

        return await GetApplicationCategories();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> AddProjectCategory(string categoryName)
    {
        await client.AddProjectCategory(categoryName);

        return await GetProjectCategories();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetApplicationCategories()
    {
        return client.GetApplicationCategories();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetProjectCategories()
    {
        return client.GetProjectCategories();
    }
}