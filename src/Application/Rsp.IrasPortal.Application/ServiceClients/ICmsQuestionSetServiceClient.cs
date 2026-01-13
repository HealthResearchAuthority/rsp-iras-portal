using Refit;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Application.ServiceClients;

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
    public Task<ApiResponse<QuestionSectionsResponse>> GetModificationPreviousQuestionSection(string currentSectionId, string? parentQuestionId = null, string? parentAnswerOption = null);

    [Get("/modificationsquestionset/getNextQuestionSection")]
    public Task<ApiResponse<QuestionSectionsResponse>> GetModificationNextQuestionSection(string currentSectionId, string? parentQuestionId = null, string? parentAnswerOption = null);

    [Get("/modificationsranking/getmodificationranking")]
    public Task<ApiResponse<RankingOfChangeResponse>> GetModificationRanking([Query] RankingOfChangeRequest rankingOfChangeRequest);
}