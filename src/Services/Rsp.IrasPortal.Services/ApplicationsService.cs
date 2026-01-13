using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services.Extensions;

namespace Rsp.Portal.Services;

public class ApplicationsService(IApplicationsServiceClient applicationsClient) : IApplicationsService
{
    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> GetProjectRecord(string projectRecordId)
    {
        var apiResponse = await applicationsClient.GetProjectRecord(projectRecordId);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplications()
    {
        var apiResponse = await applicationsClient.GetApplications();

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> GetApplicationByStatus(string applicationId, string status)
    {
        var apiResponse = await applicationsClient.GetApplicationByStatus(applicationId, status);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByStatus(string status)
    {
        var apiResponse = await applicationsClient.GetApplicationsByStatus(status);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByRespondent(string respondentId)
    {
        var apiResponse = await applicationsClient.GetApplicationsByRespondent(respondentId);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<PaginatedResponse<IrasApplicationResponse>>> GetPaginatedApplicationsByRespondent
    (
        string respondentId,
        ApplicationSearchRequest searchQuery,
        int pageIndex = 1,
        int? pageSize = 20,
        string? sortField = nameof(IrasApplicationResponse.CreatedDate),
        string? sortDirection = SortDirections.Descending
    )
    {
        var apiResponse = await applicationsClient.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize, sortField, sortDirection);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication)
    {
        var apiResponse = await applicationsClient.CreateApplication(irasApplication);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication)
    {
        var apiResponse = await applicationsClient.UpdateApplication(irasApplication);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse> DeleteProject(string projectRecordId)
    {
        var apiResponse = await applicationsClient.DeleteProject(projectRecordId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<PaginatedResponse<CompleteProjectRecordResponse>>> GetPaginatedApplications(
        ProjectRecordSearchRequest searchQuery,
        int pageIndex = 1,
        int? pageSize = 20,
        string? sortField = "CreatedDate",
        string? sortDirection = "desc")
    {
        var apiResponse = await applicationsClient.GetPaginatedApplications(
            searchQuery,
            pageIndex,
            pageSize,
            sortField,
            sortDirection);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ProjectRecordAuditTrailResponse>> GetProjectRecordAuditTrail(string projectRecordId)
    {
        var apiResponse = await applicationsClient.GetProjectRecordAuditTrail(projectRecordId);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Updates the project record status
    /// </summary>
    /// <param name="projectRecordId"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    public async Task<ServiceResponse> UpdateProjectRecordStatus(string projectRecordId, string status)
    {
        var apiResponse = await applicationsClient.UpdateProjectRecordStatus(projectRecordId, status);

        return apiResponse.ToServiceResponse();
    }
}