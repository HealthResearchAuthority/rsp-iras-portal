using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface IQuestionSetService
{
    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetInitialQuestions();

    Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetNextQuestions(string categoryId);
}