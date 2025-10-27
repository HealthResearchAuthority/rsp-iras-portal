using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class ProjectRecordValidationService(IProjectRecordValidationClient projectRecordValidationClient) : IProjectRecordValidationService
{
    public async Task<ServiceResponse<ProjectRecordValidationResponse>> ValidateProjectRecord(int irasId)
    {
        var response = await projectRecordValidationClient.ValidateProjectRecord(irasId);

        return response.ToServiceResponse();
    }
}