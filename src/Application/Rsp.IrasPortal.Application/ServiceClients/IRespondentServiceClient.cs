using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with iras microservice for respondent relation operations
/// </summary>
public interface IRespondentServiceClient
{
    /// <summary>
    /// Saves the respondent answers
    /// </summary>
    /// <returns>An asynchronous operation that saves respondent answers.</returns>
    [Post("/respondent")]
    public Task<IApiResponse> SaveRespondentAnswers(RespondentAnswersRequest request);

    /// <summary>
    /// Gets the respondent answers by applicationId
    /// </summary>
    /// <returns>An asynchronous operation that gets the respondent answers.</returns>
    [Get("/respondent/{applicationId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId);

    /// <summary>
    /// Gets the respondent answers by applicationId and categoryId
    /// </summary>
    /// <returns>An asynchronous operation that gets the respondent answers.</returns>
    [Get("/respondent/{applicationId}/{categoryId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId);
}