using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface ICmsQuestionsetService
{
    Task<ServiceResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null, bool preview = false);

    Task<ServiceResponse<IEnumerable<CategoryDto>>> GetQuestionCategories(bool preview);

    Task<ServiceResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections(bool preview);

    Task<ServiceResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId, bool preview);

    Task<ServiceResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId, bool preview);

    Task<ServiceResponse<StartingQuestionsDto>> GetInitialModificationQuestions();

    Task<ServiceResponse<CmsQuestionSetResponse>> GetModificationsJourney(string specificChangeId);

    Task<ServiceResponse<CmsQuestionSetResponse>> GetModificationQuestionSet(string? sectionId = null, string? questionSetId = null);

    Task<ServiceResponse<QuestionSectionsResponse>> GetModificationPreviousQuestionSection(string currentSectionId);

    Task<ServiceResponse<QuestionSectionsResponse>> GetModificationNextQuestionSection(string currentSectionId);
}