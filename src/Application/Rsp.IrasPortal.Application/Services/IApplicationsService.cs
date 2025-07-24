using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Applications Service Interface. Marked as IInterceptable to enable
/// the start/end logging for all methods.
/// </summary>
public interface IApplicationsService : IInterceptable
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="projectRecordId">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    public Task<ServiceResponse<IrasApplicationResponse>> GetProjectRecord(string projectRecordId);

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
    /// Gets all the saved applications for a respondent
    /// </summary>
    /// <param name="respondentId">Respondent Id associated with the application</param>
    /// <returns>An asynchronous operation that returns all the saved applications for a given respondent.</returns>
    public Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByRespondent(string respondentId);

    /// <summary>
    /// Gets all the saved applications for a respondent with pagination
    /// </summary>
    /// <param name="respondentId">Respondent Id associated with the application</param>
    /// <param name="searchQuery">Optional search query to filter projects by title or description.</param>
    /// <param name="pageIndex">Page number (1-based). Must be greater than 0.</param>
    /// <param name="pageSize">Number of records per page. Must be greater than 0.</param>
    /// <returns>An asynchronous operation that returns all the saved applications for a given respondent.</returns>
    public Task<ServiceResponse<PaginatedResponse<IrasApplicationResponse>>> GetPaginatedApplicationsByRespondent(string respondentId, string? searchQuery, int pageIndex, int pageSize);

    /// <summary>
    /// Creates a new application
    /// </summary>
    /// <param name="irasApplication">IrasApplication to be creadated</param>
    /// <returns>An asynchronous operation that returns the newly created application.</returns>
    public Task<ServiceResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication);

    /// <summary>
    /// Updates the saved application by Id
    /// </summary>
    /// <param name="irasApplication">IrasApplication to be updated</param>
    /// <returns>An asynchronous operation that updates the existing application.</returns>
    public Task<ServiceResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication);

    public Task<ServiceResponse<GetModificationsResponse>> GetModifications
    (
        ModificationSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ModificationsDto.ModificationId),
        string? sortDirection = SortDirections.Descending
    );
}