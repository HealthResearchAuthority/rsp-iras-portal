using Refit;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface IQuestionSetServiceClient
{
    /// <summary>
    /// Gets all questions in the database
    /// </summary>
    /// <returns><see cref="IEnumerable{QuestionsResponse}"/></returns>
    [Get("/questions")]
    public Task<ApiResponse<IEnumerable<QuestionsResponse>>> GetQuestions();

    /// <summary>
    /// Gets all questions in the database for the category
    /// </summary>
    [Get("/questions")]
    public Task<ApiResponse<IEnumerable<QuestionsResponse>>> GetQuestions(string categoryId);

    /// <summary>
    /// Gets all questions in the database for a version
    /// </summary>
    /// <returns><see cref="IEnumerable{QuestionsResponse}"/></returns>
    [Get("/questions/questionset")]
    public Task<ApiResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId);

    /// <summary>
    /// Gets all questions in the database for a version for the category
    /// </summary>
    /// <returns><see cref="IEnumerable{QuestionsResponse}"/></returns>
    [Get("/questions/questionset")]
    public Task<ApiResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId, string categoryId);

    /// <summary>
    /// Creates question records in the database
    /// </summary>
    [Post("/questions/questionset")]
    public Task<IApiResponse> AddQuestionSet(QuestionSetDto questionSet);

    /// <summary>
    /// Gets all versions in the database
    /// </summary>
    [Get("/questions/version/all")]
    public Task<ApiResponse<IEnumerable<VersionDto>>> GetVersions();

    /// <summary>
    /// Creates a version in the database
    /// </summary>
    [Post("/questions/version/publish")]
    public Task<IApiResponse> PublishVersion(string versionId);
}