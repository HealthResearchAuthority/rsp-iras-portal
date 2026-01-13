using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services.Extensions;

namespace Rsp.Portal.Services;

public class ProjectRecordValidationService(IProjectRecordValidationClient projectRecordValidationClient) : IProjectRecordValidationService
{
    public async Task<ServiceResponse<ProjectRecordValidationResponse>> ValidateProjectRecord(int irasId)
    {
        var response = await projectRecordValidationClient.ValidateProjectRecord(irasId);

        return response.ToServiceResponse();
    }
}