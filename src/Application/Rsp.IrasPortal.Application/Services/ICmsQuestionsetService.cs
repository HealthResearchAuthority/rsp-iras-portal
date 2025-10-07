using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

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

    Task<ServiceResponse<QuestionSectionsResponse>> GetModificationPreviousQuestionSection(string currentSectionId);

    Task<ServiceResponse<QuestionSectionsResponse>> GetModificationNextQuestionSection(string currentSectionId);

    Task<ServiceResponse<RankingOfChangeResponse>> GetModificationRanking(RankingOfChangeRequest request);
}