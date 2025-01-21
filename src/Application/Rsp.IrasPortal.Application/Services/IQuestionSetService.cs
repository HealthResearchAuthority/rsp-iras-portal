using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Questionset Service Interface
/// </summary>
public interface IQuestionSetService
{
    /// <summary>
    /// Gets all questions
    /// </summary>
    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions();

    /// <summary>
    /// Gets all questions for the category
    /// </summary>
    /// <param name="categoryId">CategoryId of the questions</param>
    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions(string categoryId);

    /// <summary>
    /// Gets all questions by version
    /// </summary>
    /// <param name="versionId">Version of the questions</param>
    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId);

    /// <summary>
    /// Gets all questions for the category
    /// </summary>
    /// <param name="categoryId">CategoryId of the questions</param>
    /// <param name="versionId">Version of the questions</param>
    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId, string categoryId);

    Task<ServiceResponse> CreateQuestions(QuestionSetDto questionSet);

    Task<ServiceResponse<IEnumerable<VersionDto>>> GetVersions();

    Task<ServiceResponse> PublishVersion(string versionId);
}