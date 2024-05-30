using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Domain.Entities;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with Applications microservice
/// </summary>
public interface IApplicationsServiceClient
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="id">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    public Task<IrasApplication> GetApplication(int id);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    public Task<IEnumerable<IrasApplication>> GetApplications();

    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="id">Application Id</param>
    /// <param name="status">Status of the application</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    public Task<ServiceResponse<IrasApplication>> GetApplicationByStatus(int id, string status);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <param name="status">Status of the application</param>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    public Task<ServiceResponse<IEnumerable<IrasApplication>>> GetApplicationsByStatus(string status);

    /// <summary>
    /// Creates a new application
    /// </summary>
    /// <returns>An asynchronous operation that returns the newly created application.</returns>
    public Task<IrasApplication> CreateApplication(IrasApplication irasApplication);

    /// <summary>
    /// Updates the saved application by Id
    /// </summary>
    /// <param name="id">Id of the application to be updated</param>
    /// <returns>An asynchronous operation that updates the existing application.</returns>
    public Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication);

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