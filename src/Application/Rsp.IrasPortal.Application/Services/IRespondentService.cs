using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services
{
    public interface IRespondentService
    {
        Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId);

        Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId);

        Task<ServiceResponse> SaveRespondentAnswers(RespondentAnswersRequest respondentAnswersRequest);
    }
}