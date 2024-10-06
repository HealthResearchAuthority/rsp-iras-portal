using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Infrastructure.HttpClients;

public interface IApplicationsHttpClient
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    [Get("/applications")]
    public Task<ApiResponse<IrasApplicationResponse>> GetApplication(string applicationId);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    [Get("/applications/all")]
    public Task<ApiResponse<IEnumerable<IrasApplicationResponse>>> GetApplications();

    /// <summary>
    /// Gets the saved application by Id and status
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <param name="status">Application Status</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    [Get("/applications/{status}")]
    public Task<ApiResponse<IrasApplicationResponse>> GetApplicationByStatus(string applicationId, string status);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    [Get("/applications/{status}/all")]
    public Task<ApiResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByStatus(string status);

    /// <summary>
    /// Creates a new application
    /// </summary>
    /// <returns>An asynchronous operation that returns the newly created application.</returns>
    [Post("/applications")]
    public Task<ApiResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication);

    /// <summary>
    /// Updates the saved application by Id
    /// </summary>
    /// <returns>An asynchronous operation that updates the existing application.</returns>
    [Post("/applications/update")]
    public Task<ApiResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication);
}