using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

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
    public async Task<ServiceResponse<PaginatedResponse<IrasApplicationResponse>>> GetPaginatedApplicationsByRespondent(string respondentId, string? searchQuery, int pageIndex, int pageSize)
    {
        var apiResponse = await applicationsClient.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageIndex, pageSize);

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

    public async Task<ServiceResponse<GetModificationsResponse>> GetModifications
    (
        ModificationSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ModificationsDto.ModificationId),
        string? sortDirection = SortDirections.Descending
    )
    {
        var apiResponse = await applicationsClient.GetModifications(searchQuery, pageNumber, pageSize, sortField, sortDirection);

        return apiResponse.ToServiceResponse();
    }
}