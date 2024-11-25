using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

/// <inheritdoc/>
public class QuestionSetService(IQuestionSetServiceClient client) : IQuestionSetService
{
    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions()
    {
        // get all questions
        var apiResponse = await client.GetQuestions();

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions(string categoryId)
    {
        // get all questions for the category
        var apiResponse = await client.GetQuestions(categoryId);

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> CreateQuestions(QuestionSetDto questionSet)
    {
        var apiResponse = await client.CreateQuestions(questionSet);

        return apiResponse.ToServiceResponse();
    }
}