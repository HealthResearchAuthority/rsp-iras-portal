using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;

namespace Rsp.Portal.Application.Services;

public interface ICmsQuestionsetService
{
    Task<ServiceResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null);

    Task<ServiceResponse<IEnumerable<CategoryDto>>> GetQuestionCategories();

    Task<ServiceResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections();

    Task<ServiceResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId);

    Task<ServiceResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId);

    Task<ServiceResponse<StartingQuestionsDto>> GetInitialModificationQuestions();

    Task<ServiceResponse<CmsQuestionSetResponse>> GetModificationsJourney(string specificChangeId);

    Task<ServiceResponse<CmsQuestionSetResponse>> GetModificationQuestionSet(string? sectionId = null, string? questionSetId = null);

    Task<ServiceResponse<QuestionSectionsResponse>> GetModificationPreviousQuestionSection(string currentSectionId, string? parentQuestionId = null, string? parentAnswerOption = null);

    Task<ServiceResponse<QuestionSectionsResponse>> GetModificationNextQuestionSection(string currentSectionId, string? parentQuestionId = null, string? parentAnswerOption = null);

    Task<ServiceResponse<RankingOfChangeResponse>> GetModificationRanking(RankingOfChangeRequest request);
}