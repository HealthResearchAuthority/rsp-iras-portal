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
    /// Gets all questions in the database for the category
    /// </summary>
    [Get("/questions")]
    public Task<ApiResponse<IEnumerable<QuestionsResponse>>> GetQuestions(string categoryId, string sectionId);

    /// <summary>
    /// Creates question records in the database
    /// </summary>
    [Post("/questions")]
    public Task<IApiResponse> CreateQuestions(QuestionSetDto questionSet);

    /// <summary>
    /// Gets all question sections in the database
    /// </summary>
    /// <returns><see cref="IEnumerable{QuestionsResponse}"/></returns>
    [Get("/questionsections/all")]
    public Task<ApiResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections();

    /// <summary>
    /// Gets all question sections in the database
    /// </summary>
    /// <returns><see cref="IEnumerable{QuestionsResponse}"/></returns>
    [Get("/questionsections/previous")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string sectionId);

    /// <summary>
    /// Gets all question sections in the database
    /// </summary>
    /// <returns><see cref="IEnumerable{QuestionsResponse}"/></returns>
    [Get("/questionsections/next")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetNextQuestionSection(string sectionId);

    /// <summary>
    /// Gets all question categories in the database
    /// </summary>
    /// <returns><see cref="IEnumerable{QuestionsResponse}"/></returns>
    [Get("/questioncatagories/all")]
    public Task<ApiResponse<IEnumerable<CategoryDto>>> GetQuestionCategories();
}