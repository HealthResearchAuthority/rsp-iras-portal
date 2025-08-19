using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class CmsQuestionsetService(ICmsQuestionSetServiceClient client) : ICmsQuestionsetService
{
    public async Task<ServiceResponse<QuestionSectionsResponse>> GetNextQuestionSection(string currentSectionId)
    {
        var responce = await client.GetNextQuestionSection(currentSectionId);

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string currentSectionId)
    {
        var responce = await client.GetPreviousQuestionSection(currentSectionId);

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<CategoryDto>>> GetQuestionCategories()
    {
        var responce = await client.GetQuestionCategories();

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections()
    {
        var responce = await client.GetQuestionSections();

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<CmsQuestionSetResponse>> GetQuestionSet(string? sectionId = null, string? questionSetId = null)
    {
        var responce = await client.GetQuestionSet(sectionId, questionSetId);

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<StartingQuestionsDto>> GetInitialModificationQuestions()
    {
        var responce = await client.GetInitialModificationQuestions();

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<QuestionSectionsResponse>> GetModificationNextQuestionSection(string currentSectionId)
    {
        var responce = await client.GetModificationNextQuestionSection(currentSectionId);

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<QuestionSectionsResponse>> GetModificationPreviousQuestionSection(string currentSectionId)
    {
        var responce = await client.GetModificationPreviousQuestionSection(currentSectionId);

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<CmsQuestionSetResponse>> GetModificationQuestionSet(string? sectionId = null, string? questionSetId = null)
    {
        var responce = await client.GetModificationQuestionSet(sectionId, questionSetId);

        // convert to service response
        return responce.ToServiceResponse();
    }

    public async Task<ServiceResponse<CmsQuestionSetResponse>> GetModificationsJourney(string specificChangeId)
    {
        var responce = await client.GetModificationsJourney(specificChangeId);

        // convert to service response
        return responce.ToServiceResponse();
    }
}