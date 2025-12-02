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
    /// Saves all respondent answers for a project modification change.
    /// </summary>
    /// <param name="request">The request containing all answers for the project modification change.</param>
    /// <returns>A service response indicating the result of the save operation.</returns>
    public async Task<ServiceResponse> SaveModificationChangeAnswers(ProjectModificationChangeAnswersRequest request)
    {
        var apiResponse = await respondentServiceClient.SaveModificationChangeAnswers(request);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for a specific project modification change.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier for the project modification change.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationChangeAnswers(Guid modificationChangeId, string projectRecordId)
    {
        var apiResponse = await respondentServiceClient.GetModificationChangeAnswers(modificationChangeId, projectRecordId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for a specific project modification change and category.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier for the project modification change.</param>
    /// <param name="categoryId">The unique identifier for the question category.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationChangeAnswers(Guid modificationChangeId, string projectRecordId, string categoryId)
    {
        var apiResponse = await respondentServiceClient.GetModificationChangeAnswers(modificationChangeId, projectRecordId, categoryId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for a specific project modification.
    /// </summary>
    /// <param name="modificationId">The unique identifier for the project modification change.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid modificationId, string projectRecordId)
    {
        var apiResponse = await respondentServiceClient.GetModificationAnswers(modificationId, projectRecordId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Gets all respondent answers for a specific project modification and category.
    /// </summary>
    /// <param name="modificationId">The unique identifier for the project modification.</param>
    /// <param name="categoryId">The unique identifier for the question category.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    public async Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid modificationId, string projectRecordId, string categoryId)
    {
        var apiResponse = await respondentServiceClient.GetModificationAnswers(modificationId, projectRecordId, categoryId);
        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Retrieves all documents associated with a specific project modification change,
    /// filtered by the project record ID and the project personnel who submitted or are responsible for the documents.
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project modification change.</param>
    /// <param name="projectRecordId">The unique identifier of the associated project record.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation,
    /// containing a <see cref="ServiceResponse{T}"/> with a collection of <see cref="ProjectModificationDocumentRequest"/>
    /// representing the associated documents.
    /// </returns>
    public async Task<ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>> GetModificationChangesDocuments(Guid modificationId, string projectRecordId)
    {
        var apiResponse = await respondentServiceClient.GetModificationChangesDocuments(modificationId, projectRecordId);

        return apiResponse.ToServiceResponse();
    }

    /// <summary>
    /// Retrieves all documents associated with a specific project modification change,
    /// filtered by the project record ID and the user who submitted or are responsible for the documents.
    /// </summary>
    /// <param name="modificationId">The unique identifier of the project modification change.</param>
    /// <param name="projectRecordId">The unique identifier of the associated project record.</param>
    /// <param name="userId">The unique identifier of the user who uploaded or is linked to the documents.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation,
    /// containing a <see cref="ServiceResponse{T}"/> with a collection of <see cref="ProjectModificationDocumentRequest"/>
    /// representing the associated documents.
    /// </returns>
    public async Task<ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>> GetModificationChangesDocuments(Guid modificationId, string projectRecordId, string userId)
    {
        var apiResponse = await respondentServiceClient.GetModificationChangesDocuments(modificationId, projectRecordId, userId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ProjectModificationDocumentRequest>> GetModificationDocumentDetails(Guid documentId)
    {
        var apiResponse = await respondentServiceClient.GetModificationDocumentDetails(documentId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> SaveModificationDocuments(List<ProjectModificationDocumentRequest> request)
    {
        var apiResponse = await respondentServiceClient.SaveModificationDocuments(request);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> SaveModificationDocumentAnswers(List<ProjectModificationDocumentAnswerDto> request)
    {
        var apiResponse = await respondentServiceClient.SaveModificationDocumentAnswer(request);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>> GetModificationDocumentAnswers(Guid documentId)
    {
        var apiResponse = await respondentServiceClient.GetModificationDocumentAnswers(documentId);

        return apiResponse.ToServiceResponse();
    }
}