using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class QuestionSetService(IQuestionSetServiceClient client) : IQuestionSetService
{
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetInitialQuestions()
    {
        var apiGetQuestionsResponse = await client.GetInitialQuestions();

        return apiGetQuestionsResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetNextQuestions(string categoryId)
    {
        var apiGetQuestionsResponse = await client.GetNextQuestions(categoryId);

        return apiGetQuestionsResponse.ToServiceResponse();
    }

    // create questions passing in QuestionDto
    public async Task<ServiceResponse> CreateQuestions(IEnumerable<QuestionDto> questions)
    {
        var apiResponse = await client.CreateQuestions(questions);

        return apiResponse.ToServiceResponse();
    }
}