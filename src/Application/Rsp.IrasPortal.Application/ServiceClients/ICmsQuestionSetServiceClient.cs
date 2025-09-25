using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsQuestionSetServiceClient
{
    [Get("/projectRecordQuestionset/getQuestionSet")]
    public Task<ApiResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null, bool preview = false);

    [Get("/projectRecordQuestionset/getQuestionCategories")]
    public Task<ApiResponse<IEnumerable<CategoryDto>>> GetQuestionCategories(bool preview);

    [Get("/projectRecordQuestionset/getQuestionSections")]
    public Task<ApiResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections(bool preview);

    [Get("/projectRecordQuestionset/getPreviousQuestionSection")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId, bool preview);

    [Get("/projectRecordQuestionset/getNextQuestionSection")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId, bool preview);

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