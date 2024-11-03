using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class RespondentService(IRespondentServiceClient respondentServiceClient) : IRespondentService
{
    public async Task<ServiceResponse> SaveRespondentAnswers(RespondentAnswersRequest respondentAnswersRequest)
    {
        var apiResponse = await respondentServiceClient.SaveRespondentAnswers(respondentAnswersRequest);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId)
    {
        var apiResponse = await respondentServiceClient.GetRespondentAnswers(applicationId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId)
    {
        var apiResponse = await respondentServiceClient.GetRespondentAnswers(applicationId, categoryId);

        return apiResponse.ToServiceResponse();
    }
}