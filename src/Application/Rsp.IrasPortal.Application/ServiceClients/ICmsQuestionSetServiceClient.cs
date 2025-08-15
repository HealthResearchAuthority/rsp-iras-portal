using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsQuestionSetServiceClient
{
    [Get("/umbraco/api/projectRecordQuestionset/getQuestionSet?sectionId={sectionId}&questionSetId={questionSetId}")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null);

    [Get("/umbraco/api/projectRecordQuestionset/getQuestionCategories")]
    public Task<ApiResponse<IEnumerable<CategoryDto>>> GetQuestionCategories();

    [Get("/umbraco/api/projectRecordQuestionset/getQuestionSections")]
    public Task<ApiResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections();

    [Get("/umbraco/api/projectRecordQuestionset/getPreviousQuestionSection?currentSectionId={currentSectionId}")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId);

    [Get("/umbraco/api/projectRecordQuestionset/getNextQuestionSection?currentSectionId={currentSectionId}")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId);

    [Get("/umbraco/api/modificationsquestionset/getstartingquestions")]
    public Task<ApiResponse<StartingQuestionsModel>> GetInitialModificationQuestions();

    [Get("/umbraco/api/modificationsquestionset/GetModificationsJourney?specificChangeId={specificChangeId}")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetModificationsJourney(string specificChangeId);
}