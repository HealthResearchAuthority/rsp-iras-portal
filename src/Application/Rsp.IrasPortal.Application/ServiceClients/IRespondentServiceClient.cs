using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with iras microservice for respondent relation operations.
/// Provides methods for saving and retrieving respondent and modification answers.
/// </summary>
public interface IRespondentServiceClient
{
    /// <summary>
    /// Saves the respondent answers.
    /// </summary>
    /// <param name="request">The respondent answers request.</param>
    /// <returns>An asynchronous operation that saves respondent answers.</returns>
    [Post("/respondent")]
    public Task<IApiResponse> SaveRespondentAnswers(RespondentAnswersRequest request);

    /// <summary>
    /// Saves the modification answers.
    /// </summary>
    /// <param name="request">The project modification answers request.</param>
    /// <returns>An asynchronous operation that saves modification answers.</returns>
    [Post("/respondent/modification")]
    public Task<IApiResponse> SaveModificationAnswers(ProjectModificationAnswersRequest request);

    /// <summary>
    /// Saves the modification answers.
    /// </summary>
    /// <param name="request">The project modification answers request.</param>
    /// <returns>An asynchronous operation that saves modification answers.</returns>
    [Post("/respondent/modificationchange")]
    public Task<IApiResponse> SaveModificationChangeAnswers(ProjectModificationChangeAnswersRequest request);

    /// <summary>
    /// Gets the respondent answers by applicationId.
    /// </summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <returns>An asynchronous operation that gets the respondent answers.</returns>
    [Get("/respondent/{applicationId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId);

    /// <summary>
    /// Gets the respondent answers by applicationId and categoryId.
    /// </summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>An asynchronous operation that gets the respondent answers.</returns>
    [Get("/respondent/{applicationId}/{categoryId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetRespondentAnswers(string applicationId, string categoryId);

    /// <summary>
    /// Gets the modification answers by project modification change Id.
    /// </summary>
    /// <param name="modificationChangeId">The project modification change identifier.</param>
    /// <returns>An asynchronous operation that gets the modification change answers.</returns>
    [Get("/respondent/modificationchange/{modificationChangeId}/{projectRecordId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetModificationChangeAnswers(Guid modificationChangeId, string projectRecordId);

    /// <summary>
    /// Gets the modification answers by project modification change Id and categoryId.
    /// </summary>
    /// <param name="modificationChangeId">The project modification change identifier.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>An asynchronous operation that gets the modification change answers.</returns>
    [Get("/respondent/modificationchange/{modificationChangeId}/{projectRecordId}/{categoryId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetModificationChangeAnswers(Guid modificationChangeId, string projectRecordId, string categoryId);

    /// <summary>
    /// Gets the modification answers by project modification Id.
    /// </summary>
    /// <param name="modificationId">The project modification identifier.</param>
    /// <returns>An asynchronous operation that gets the modification answers.</returns>
    [Get("/respondent/modification/{modificationId}/{projectRecordId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid modificationId, string projectRecordId);

    /// <summary>
    /// Gets the modification answers by project modification Id and categoryId.
    /// </summary>
    /// <param name="modificationId">The project modification identifier.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>An asynchronous operation that gets the modification answers.</returns>
    [Get("/respondent/modification/{modificationId}/{projectRecordId}/{categoryId}")]
    public Task<ApiResponse<IEnumerable<RespondentAnswerDto>>> GetModificationAnswers(Guid modificationId, string projectRecordId, string categoryId);

    /// <summary>
    /// Retrieves all modification documents associated with a specific project modification change and respondent.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier for the project modification change.</param>
    /// <param name="projectRecordId">The identifier of the associated project record.</param>
    /// <param name="projectPersonnelId">The identifier of the personnel who uploaded or is associated with the documents.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of <see cref="ProjectModificationDocumentRequest"/> wrapped in an <see cref="ApiResponse{T}"/>.
    /// </returns>
    [Get("/respondent/modificationdocument/{modificationChangeId}/{projectRecordId}")]
    public Task<ApiResponse<IEnumerable<ProjectModificationDocumentRequest>>> GetModificationChangesDocuments(Guid modificationChangeId, string projectRecordId);

    /// <summary>
    /// Retrieves all modification documents associated with a specific project modification change and respondent.
    /// </summary>
    /// <param name="modificationChangeId">The unique identifier for the project modification change.</param>
    /// <param name="projectRecordId">The identifier of the associated project record.</param>
    /// <param name="projectPersonnelId">The identifier of the personnel who uploaded or is associated with the documents.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of <see cref="ProjectModificationDocumentRequest"/> wrapped in an <see cref="ApiResponse{T}"/>.
    /// </returns>
    [Get("/respondent/modificationdocument/{modificationChangeId}/{projectRecordId}/{projectPersonnelId}")]
    public Task<ApiResponse<IEnumerable<ProjectModificationDocumentRequest>>> GetModificationChangesDocuments(Guid modificationChangeId, string projectRecordId, string projectPersonnelId);

    [Get("/respondent/modificationdocumentdetails/{documentId}")]
    public Task<ApiResponse<ProjectModificationDocumentRequest>> GetModificationDocumentDetails(Guid documentId);

    /// <summary>
    /// Retrieves all modification documents associated with a specific project modification change and respondent.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of <see cref="ProjectModificationDocumentRequest"/> wrapped in an <see cref="ApiResponse{T}"/>.
    /// </returns>
    [Post("/respondent/modificationdocument")]
    public Task<IApiResponse> SaveModificationDocuments(List<ProjectModificationDocumentRequest> projectModificationDocumentRequest);

    [Get("/respondent/modificationdocumentanswer/{documentId}")]
    public Task<ApiResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>> GetModificationDocumentAnswers(Guid documentId);

    [Post("/respondent/modificationdocumentanswer")]
    public Task<IApiResponse> SaveModificationDocumentAnswer(List<ProjectModificationDocumentAnswerDto> projectModificationDocumentRequest);
}