using Refit;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsQuestionSetServiceClient
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    [Get("/umbraco/api/NestedContent/GetQuestionSet?questionSetId={questionSetId}")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetQuestionSet(string questionSetId);
}