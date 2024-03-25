namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Interface for the categories service that utilizes the <see cref="ICategoriesServiceClient"/>.
/// </summary>
public interface ICategoriesService
{
    /// <summary>
    /// Retrieves the list of application categories.
    /// </summary>
    /// <returns>An asynchronous operation that returns a collection of strings representing application categories.</returns>
    public Task<IEnumerable<string>> GetApplicationCategories();

    /// <summary>
    /// Retrieves the list of project categories.
    /// </summary>
    /// <returns>An asynchronous operation that returns a collection of strings representing project categories.</returns>
    public Task<IEnumerable<string>> GetProjectCategories();

    /// <summary>
    /// Adds a new application category.
    /// </summary>
    /// <param name="categoryName">The name of the category to be added.</param>
    /// <returns>An asynchronous operation that returns a collection of strings representing updated application categories.</returns>
    public Task<IEnumerable<string>> AddApplicationCategory(string categoryName);

    /// <summary>
    /// Adds a new project category.
    /// </summary>
    /// <param name="categoryName">The name of the category to be added.</param>
    /// <returns>An asynchronous operation that returns a collection of strings representing updated project categories.</returns>
    public Task<IEnumerable<string>> AddProjectCategory(string categoryName);
}