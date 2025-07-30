using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Respondent Service Interface. Marked as IInterceptable to enable
/// the start/end logging for all methods.
/// </summary>
public interface IRespondentService : IInterceptable
{
    /// <summary>
    /// Gets all the respondent's answers for the specified application.
    /// </summary>
    /// <param name="applicationId">The unique identifier for the application.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId);

    /// <summary>
    /// Gets all the respondent's answers for the specified application and category.
    /// </summary>
    /// <param name="applicationId">The unique identifier for the application.</param>
    /// <param name="categoryId">The unique identifier for the question category.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId);

    /// <summary>
    /// Gets all the respondent's answers for a specific project modification change.
    /// </summary>
    /// <param name="projectModificationChangeId">The unique identifier for the project modification change.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid projectModificationChangeId);

    /// <summary>
    /// Gets all the respondent's answers for a specific project modification change and category.
    /// </summary>
    /// <param name="projectModificationChangeId">The unique identifier for the project modification change.</param>
    /// <param name="categoryId">The unique identifier for the question category.</param>
    /// <returns>A service response containing a collection of respondent answers.</returns>
    Task<ServiceResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid projectModificationChangeId, string categoryId);

    /// <summary>
    /// Saves all the respondent's answers for a project modification.
    /// </summary>
    /// <param name="request">The request containing all answers for the project modification.</param>
    /// <returns>A service response indicating the result of the save operation.</returns>
    Task<ServiceResponse> SaveModificationAnswers(ProjectModificationAnswersRequest request);

    /// <summary>
    /// Saves all the respondent's answers for the application and category.
    /// </summary>
    /// <param name="respondentAnswersRequest">The request containing all answers for the application and category.</param>
    /// <returns>A service response indicating the result of the save operation.</returns>
    Task<ServiceResponse> SaveRespondentAnswers(RespondentAnswersRequest respondentAnswersRequest);

    /// <summary>
    /// Retrieves all documents associated with a specific project modification change,
    /// filtered by the project record ID and the project personnel who submitted or are responsible for the documents.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier of the project modification change.</param>
    /// <param name="projectRecordId">The unique identifier of the associated project record.</param>
    /// <param name="projectPersonnelId">The unique identifier of the project personnel who uploaded or is linked to the documents.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation,
    /// containing a <see cref="ServiceResponse{T}"/> with a collection of <see cref="ProjectModificationDocumentRequest"/>
    /// representing the associated documents.
    /// </returns>
    Task<ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>> GetModificationChangesDocuments(Guid modificationChangeId, string projectRecordId, string projectPersonnelId);
}