using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsQuestionSetServiceClient
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    [Get("/umbraco/api/NestedContent/GetQuestionSet?sectionId={sectionId}&questionSetId={questionSetId}")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null);

    [Get("/umbraco/api/NestedContent/GetQuestionCategories")]
    public Task<ApiResponse<IEnumerable<CategoryDto>>> GetQuestionCategories();

    [Get("/umbraco/api/NestedContent/GetQuestionSections")]
    public Task<ApiResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections();

    [Get("/umbraco/api/NestedContent/GetPreviousQuestionSection?currentSectionId={currentSectionId}")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId);

    [Get("/umbraco/api/NestedContent/GetNextQuestionSection?currentSectionId={currentSectionId}")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId);
}