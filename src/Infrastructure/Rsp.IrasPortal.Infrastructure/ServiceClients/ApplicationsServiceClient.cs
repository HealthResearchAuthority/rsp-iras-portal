using Rsp.IrasPortal.Application.Responses;
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
    public async Task<ServiceResponse<IrasApplication>> GetApplicationByStatus(int id, string status)
    {
        var apiResponse = await client.GetApplicationByStatus(id, status);

        var serviceResponse = new ServiceResponse<IrasApplication>();

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

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplication>>> GetApplicationsByStatus(string status)
    {
        var apiResponse = await client.GetApplicationsByStatus(status);

        var serviceResponse = new ServiceResponse<IEnumerable<IrasApplication>>();

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