using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Services;

/// <summary>
/// Implementation of <see cref="ICategoriesService"/> that utilizes an <see cref="ICategoriesServiceClient"/>.
/// </summary>
public class CategoriesService(ICategoriesServiceClient categoriesClient) : ICategoriesService
{
    /// <inheritdoc/>
    public Task<IEnumerable<string>> AddApplicationCategory(string categoryName)
    {
        return categoriesClient.AddApplicationCategory(categoryName);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> AddProjectCategory(string categoryName)
    {
        return categoriesClient.AddProjectCategory(categoryName);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetApplicationCategories()
    {
        return categoriesClient.GetApplicationCategories();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetProjectCategories()
    {
        return categoriesClient.GetProjectCategories();
    }
}