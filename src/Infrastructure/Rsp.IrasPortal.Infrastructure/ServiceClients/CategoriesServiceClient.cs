using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Infrastructure.HttpClients;

namespace Rsp.IrasPortal.Infrastructure.ServiceClients;

/// <summary>
/// Implementation of <see cref="ICategoriesServiceClient"/> using <see cref="ICategoriesHttpClient"/>.
/// </summary>
public class CategoriesServiceClient(ICategoriesHttpClient client) : ICategoriesServiceClient
{
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