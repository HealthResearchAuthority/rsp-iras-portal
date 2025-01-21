using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Questionset Service Interface. Marked as IInterceptable to enable
/// the start/end logging for all methods.
/// </summary>
public interface IQuestionSetService : IInterceptable
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
    /// Gets all questions for the category and section
    /// </summary>
    /// <param name="categoryId">Category Id of the questions</param>
    /// <param name="sectionId">Section Id of the questions</param>
    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions(string categoryId, string sectionId);

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

    /// <summary>
    /// Gets all questions for the category
    /// </summary>
    /// <param name="categoryId">CategoryId of the questions</param>
    /// <param name="sectionId">SectionId of the questions</param>
    /// <param name="versionId">Version of the questions</param>
    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId, string categoryId, string sectionId);

    /// <summary>
    /// Gets previous question sections
    /// </summary>
    Task<ServiceResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections();

    /// <summary>
    /// Gets all question sections in the database
    /// </summary>
    Task<ServiceResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string sectionId);

    /// <summary>
    /// Gets next  question sections in the database
    /// </summary>
    Task<ServiceResponse<QuestionSectionsResponse>> GetNextQuestionSection(string sectionId);

    /// <summary>
    /// Gets all question sections
    /// </summary>
    Task<ServiceResponse<IEnumerable<CategoryDto>>> GetQuestionCategories();

    /// <summary>
    /// Parses an uploaded question set file and return a QuestionSetDto
    /// </summary>
    /// <param name="file">A question set file</param>
    ServiceResponse<QuestionSetDto> ProcessQuestionSetFile(IFormFile file);

    /// <summary>
    /// Creates questions, sections, categories, answer options,
    /// rules, and version records in the database for a question set
    /// </summary>
    /// <param name="questionSet">The question set data</param>
    Task<ServiceResponse> AddQuestionSet(QuestionSetDto questionSet);

    /// <summary>
    /// Gets all question set versions
    /// </summary>
    Task<ServiceResponse<IEnumerable<VersionDto>>> GetVersions();

    /// <summary>
    /// Publishes a question set version
    /// </summary>
    /// <param name="versionId">The versionId of the question set to publish</param>
    Task<ServiceResponse> PublishVersion(string versionId);
}