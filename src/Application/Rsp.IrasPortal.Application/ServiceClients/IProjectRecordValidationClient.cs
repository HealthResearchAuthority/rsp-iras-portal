using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with Iras Validation Azure Function via HttpTrigger.
/// </summary>

public interface IProjectRecordValidationClient
{
    [Get("/projectrecord/validate")]
    Task<ApiResponse<ProjectRecordValidationResponse>> ValidateProjectRecord(int irasId);
}