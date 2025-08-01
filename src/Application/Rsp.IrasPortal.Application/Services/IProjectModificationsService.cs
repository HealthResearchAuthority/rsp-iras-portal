using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Service interface for managing project modifications.
/// Marked as IInterceptable to enable start/end logging for all methods.
/// </summary>
public interface IProjectModificationsService : IInterceptable
{
    /// <summary>
    /// Retrieves a specific project modification by project record ID and modification ID.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="projectModificationId">The unique identifier of the project modification.</param>
    /// <returns>A service response containing the project modification details.</returns>
    Task<ServiceResponse<ProjectModificationResponse>> GetModification(string projectRecordId, Guid projectModificationId);

    /// <summary>
    /// Retrieves all modifications for a given project record.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <returns>A service response containing a collection of project modifications.</returns>
    Task<ServiceResponse<IEnumerable<ProjectModificationResponse>>> GetModifications(string projectRecordId);

    /// <summary>
    /// Retrieves all modifications for a given project record filtered by status.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="status">The status to filter modifications by.</param>
    /// <returns>A service response containing a collection of project modifications with the specified status.</returns>
    Task<ServiceResponse<IEnumerable<ProjectModificationResponse>>> GetModificationsByStatus(string projectRecordId, string status);

    /// <summary>
    /// Creates a new project modification.
    /// </summary>
    /// <param name="projectModificationRequest">The request object containing details for the new modification.</param>
    /// <returns>An asynchronous operation that returns the newly created project modification.</returns>
    Task<ServiceResponse<ProjectModificationResponse>> CreateModification(ProjectModificationRequest projectModificationRequest);

    /// <summary>
    /// Creates a new change for an existing project modification.
    /// </summary>
    /// <param name="projectModificationChangeRequest">The request object containing details for the modification change.</param>
    /// <returns>An asynchronous operation that returns the updated project modification change.</returns>
    Task<ServiceResponse<ProjectModificationChangeResponse>> CreateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest);

    /// <summary>
    /// Gets all the area of changes and specific area of changes for the modification.
    /// </summary>
    Task<ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>> GetAreaOfChanges();

    Task<ServiceResponse<StartingQuestionsModel>> GetInitialQuestions();
    /// Creates one or more project modification documents based on the provided request data.
    /// </summary>
    /// <param name="projectModificationDocumentRequest">
    /// A list of <see cref="ProjectModificationDocumentRequest"/> objects containing the details of each document to be created,
    /// such as file metadata, modification change identifiers, and document type associations.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation,
    /// containing a <see cref="ServiceResponse"/> indicating success or failure of the creation process.
    /// </returns>
    Task<ServiceResponse> CreateDocumentModification(List<ProjectModificationDocumentRequest> projectModificationDocumentRequest);
}