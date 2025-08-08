using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface ICmsQuestionsetService
{
    Task<ServiceResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null);

    Task<ServiceResponse<IEnumerable<CategoryDto>>> GetQuestionCategories();

    Task<ServiceResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections();

    Task<ServiceResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId);

    Task<ServiceResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId);

    Task<ServiceResponse<StartingQuestionsModel>> GetInitialModificationQuestions();

    Task<ServiceResponse<CmsQuestionSetResponse>> GetModificationsJourney(string specificChangeId);
}