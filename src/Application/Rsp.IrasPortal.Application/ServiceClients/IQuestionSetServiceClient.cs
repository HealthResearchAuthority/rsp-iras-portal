using Refit;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface IQuestionSetServiceClient
{
    /// <summary>
    /// Gets all the roles in the database
    /// </summary>
    /// <returns>List of roles</returns>
    [Get("/questions")]
    public Task<ApiResponse<IEnumerable<QuestionsResponse>>> GetInitialQuestions();

    /// <summary>
    /// Creates a new role in the database
    /// </summary>
    [Get("/questions/next")]
    public Task<ApiResponse<IEnumerable<QuestionsResponse>>> GetNextQuestions(string category);

    /// <summary>
    /// Creates question records in the database
    /// </summary>
    [Post("/questions")]
    public Task<IApiResponse> CreateQuestions(IEnumerable<QuestionDto> questions);
}