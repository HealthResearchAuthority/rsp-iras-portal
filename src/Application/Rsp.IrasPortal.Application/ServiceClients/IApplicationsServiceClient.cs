using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasService.Application.DTOS.Requests;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with Applications microservice
/// </summary>
public interface IApplicationsServiceClient
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    public Task<ServiceResponse<IrasApplicationResponse>> GetApplication(string applicationId);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    public Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplications();

    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <param name="status">Status of the application</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    public Task<ServiceResponse<IrasApplicationResponse>> GetApplicationByStatus(string applicationId, string status);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <param name="status">Status of the application</param>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    public Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByStatus(string status);

    /// <summary>
    /// Creates a new application
    /// </summary>
    /// <returns>An asynchronous operation that returns the newly created application.</returns>
    public Task<ServiceResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication);

    /// <summary>
    /// Updates the saved application by Id
    /// </summary>
    /// <returns>An asynchronous operation that updates the existing application.</returns>
    public Task<ServiceResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication);
}