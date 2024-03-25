using Refit;

namespace Rsp.IrasPortal.Infrastructure.HttpClients;

/// <summary>
/// Interface for interacting with the categories microservice using HttpClient.
/// </summary>
public interface ICategoriesHttpClient
{
    /// <summary>
    /// Retrieves the list of application categories.
    /// </summary>
    /// <returns>An asynchronous operation that returns a collection of strings representing application categories.</returns>
    [Get("/categories/apps")]
    public Task<IEnumerable<string>> GetApplicationCategories();

    /// <summary>
    /// Retrieves the list of project categories.
    /// </summary>
    /// <returns>An asynchronous operation that returns a collection of strings representing project categories.</returns>
    [Get("/categories/projects")]
    public Task<IEnumerable<string>> GetProjectCategories();

    /// <summary>
    /// Adds a new application category.
    /// </summary>
    /// <param name="category">The name of the category to be added.</param>
    /// <returns>An asynchronous operation.</returns>
    [Post("/categories/apps")]
    public Task AddApplicationCategory(string category);

    /// <summary>
    /// Adds a new project category.
    /// </summary>
    /// <param name="category">The name of the category to be added.</param>
    /// <returns>An asynchronous operation.</returns>
    [Post("/categories/projects")]
    public Task AddProjectCategory(string category);
}