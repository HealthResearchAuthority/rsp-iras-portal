using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsQuestionSetServiceClient
{
    [Get("/projectRecordQuestionset/getQuestionSet")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null);

    [Get("/projectRecordQuestionset/getQuestionCategories")]
    public Task<ApiResponse<IEnumerable<CategoryDto>>> GetQuestionCategories();

    [Get("/projectRecordQuestionset/getQuestionSections")]
    public Task<ApiResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections();

    [Get("/projectRecordQuestionset/getPreviousQuestionSection")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId);

    [Get("/projectRecordQuestionset/getNextQuestionSection")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId);

    [Get("/modificationsquestionset/getstartingquestions")]
    public Task<ApiResponse<StartingQuestionsDto>> GetInitialModificationQuestions();

    [Get("/modificationsquestionset/GetModificationsJourney")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetModificationsJourney(string specificChangeId);

    [Get("/modificationsquestionset/getQuestionSet")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetModificationQuestionSet(string? sectionId = null, string? questionSetId = null);

    [Get("/modificationsquestionset/getPreviousQuestionSection")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetModificationPreviousQuestionSection(string currentSectionId);

    [Get("/modificationsquestionset/getNextQuestionSection")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetModificationNextQuestionSection(string currentSectionId);
}