using Refit;
using Rsp.IrasPortal.Domain.Entities;

namespace Rsp.IrasPortal.Infrastructure.HttpClients;

public interface IApplicationsHttpClient
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="id">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    [Get("/applications")]
    public Task<IrasApplication> GetApplication(int id);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    [Get("/applications/all")]
    public Task<IEnumerable<IrasApplication>> GetApplications();

    /// <summary>
    /// Creates a new application
    /// </summary>
    /// <returns>An asynchronous operation that returns the newly created application.</returns>
    [Post("/applications")]
    public Task<IrasApplication> CreateApplication(IrasApplication irasApplication);

    /// <summary>
    /// Updates the saved application by Id
    /// </summary>
    /// <param name="id">Id of the application to be updated</param>
    /// <returns>An asynchronous operation that updates the existing application.</returns>
    [Post("/applications/update")]
    public Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication);

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