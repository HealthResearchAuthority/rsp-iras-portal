using Refit;
using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Application.ServiceClients;

/// <summary>
/// Interface to interact with Iras Validation Azure Function via HttpTrigger.
/// </summary>

public interface IProjectRecordValidationClient
{
    [Get("/projectrecord/validate")]
    Task<ApiResponse<ProjectRecordValidationResponse>> ValidateProjectRecord(int irasId);
}