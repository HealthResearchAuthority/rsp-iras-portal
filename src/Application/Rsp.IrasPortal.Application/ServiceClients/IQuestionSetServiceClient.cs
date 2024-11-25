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
    /// Creates question records in the database
    /// </summary>
    [Post("/questions")]
    public Task<IApiResponse> CreateQuestions(QuestionSetDto questionSet);
}