using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.Portal.Application.Services;

public interface IProjectRecordValidationService : IInterceptable
{
    Task<ServiceResponse<ProjectRecordValidationResponse>> ValidateProjectRecord(int irasId);
}