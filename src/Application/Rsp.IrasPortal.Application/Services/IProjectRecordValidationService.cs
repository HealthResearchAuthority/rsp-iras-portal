using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

public interface IProjectRecordValidationService : IInterceptable
{
    Task<ServiceResponse<ProjectRecordValidationResponse>> ValidateProjectRecord(int irasId);
}