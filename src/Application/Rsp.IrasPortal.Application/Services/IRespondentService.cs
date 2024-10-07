using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Respondent Service Interface
/// </summary>
public interface IRespondentService
{
    /// <summary>
    /// Gets all the respondent's answers for the application
    /// </summary>
    /// <param name="applicationId">Iras Id</param>
    Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId);

    /// <summary>
    /// Gets all the respondent's answers for the application and category
    /// </summary>
    /// <param name="applicationId">Iras Id</param>
    /// <param name="categoryId">Category Id of the questions</param>
    Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId);

    /// <summary>
    /// Saves all the respondent's answers
    /// </summary>
    /// <param name="respondentAnswersRequest">Respondent answers request that contains all the answers for the application and category</param>
    Task<ServiceResponse> SaveRespondentAnswers(RespondentAnswersRequest respondentAnswersRequest);
}