using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with iras microservice for respondent relation operations.
/// Provides methods for saving and retrieving respondent and modification answers.
/// </summary>
public interface IRespondentServiceClient
{
    /// <summary>
    /// Saves the respondent answers.
    /// </summary>
    /// <param name="request">The respondent answers request.</param>
    /// <returns>An asynchronous operation that saves respondent answers.</returns>
    [Post("/respondent")]
    public Task<IApiResponse> SaveRespondentAnswers(RespondentAnswersRequest request);

    /// <summary>
    /// Saves the modification answers.
    /// </summary>
    /// <param name="request">The project modification answers request.</param>
    /// <returns>An asynchronous operation that saves modification answers.</returns>
    [Post("/respondent/modification")]
    public Task<IApiResponse> SaveModificationAnswers(ProjectModificationAnswersRequest request);

    /// <summary>
    /// Gets the respondent answers by applicationId.
    /// </summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <returns>An asynchronous operation that gets the respondent answers.</returns>
    [Get("/respondent/{applicationId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId);

    /// <summary>
    /// Gets the respondent answers by applicationId and categoryId.
    /// </summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>An asynchronous operation that gets the respondent answers.</returns>
    [Get("/respondent/{applicationId}/{categoryId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId);

    /// <summary>
    /// Gets the modification answers by project modification change Id.
    /// </summary>
    /// <param name="projectModificationChangeId">The project modification change identifier.</param>
    /// <returns>An asynchronous operation that gets the modification answers.</returns>
    [Get("/respondent/modification/{projectModificationChangeId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid projectModificationChangeId);

    /// <summary>
    /// Gets the modification answers by project modification change Id and categoryId.
    /// </summary>
    /// <param name="projectModificationChangeId">The project modification change identifier.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>An asynchronous operation that gets the modification answers.</returns>
    [Get("/respondent/{projectModificationChangeId}/{categoryId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid projectModificationChangeId, string categoryId);
}