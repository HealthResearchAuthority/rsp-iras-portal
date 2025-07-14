using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

/// <summary>
/// Service for handling respondent answers and project modification answers.
/// Implements <see cref="IRespondentService"/> and delegates operations to <see cref="IRespondentServiceClient"/>.
/// </summary>
public class RespondentService(IRespondentServiceClient respondentServiceClient) : IRespondentService
{
    /// <summary>
    /// Saves respondent answers for an application and category.
    /// </summary>
    /// <param name="respondentAnswersRequest">The request containing respondent answers.</param>
    /// <returns>A service response indicating the result of the save operation.</returns>
    public async Task<ServiceResponse> SaveRespondentAnswers(RespondentAnswersRequest respondentAnswersRequest)
    {
        var apiResponse = await respondentServiceClient.SaveRespondentAnswers(respondentAnswersRequest);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for the specified application.
    /// </summary>
    /// <param name="applicationId">The unique identifier for the application.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId)
    {
        var apiResponse = await respondentServiceClient.GetRespondentAnswers(applicationId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for the specified application and category.
    /// </summary>
    /// <param name="applicationId">The unique identifier for the application.</param>
    /// <param name="categoryId">The unique identifier for the question category.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId)
    {
        var apiResponse = await respondentServiceClient.GetRespondentAnswers(applicationId, categoryId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Saves all respondent answers for a project modification.
    /// </summary>
    /// <param name="request">The request containing all answers for the project modification.</param>
    /// <returns>A service response indicating the result of the save operation.</returns>
    public async Task<ServiceResponse> SaveModificationAnswers(ProjectModificationAnswersRequest request)
    {
        var apiResponse = await respondentServiceClient.SaveModificationAnswers(request);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for a specific project modification change.
    /// </summary>
    /// <param name="projectModificationChangeId">The unique identifier for the project modification change.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid projectModificationChangeId)
    {
        var apiResponse = await respondentServiceClient.GetModificationAnswers(projectModificationChangeId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for a specific project modification change and category.
    /// </summary>
    /// <param name="projectModificationChangeId">The unique identifier for the project modification change.</param>
    /// <param name="categoryId">The unique identifier for the question category.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid projectModificationChangeId, string categoryId)
    {
        var apiResponse = await respondentServiceClient.GetModificationAnswers(projectModificationChangeId, categoryId);
        return apiResponse.ToServiceResponse();
    }
}