using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with the Iras microservice.
/// Provides methods to retrieve, create, and manage project modification records.
/// </summary>
public interface IProjectModificationsServiceClient
{
    /// <summary>
    /// Gets the saved project modification by project record Id and modification Id.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="projectModificationId">The unique identifier of the project modification.</param>
    /// <returns>An asynchronous operation that returns the saved project modification.</returns>
    [Get("/projectmodifications/{projectRecordId}")]
    public Task<ApiResponse<ProjectModificationResponse>> GetModification(string projectRecordId, Guid projectModificationId);

    /// <summary>
    /// Gets all saved project modifications for a given project record Id.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <returns>An asynchronous operation that returns all saved project modifications for the specified project record.</returns>
    [Get("/projectmodifications")]
    public Task<ApiResponse<IEnumerable<ProjectModificationResponse>>> GetModifications(string projectRecordId);

    /// <summary>
    /// Gets all saved project modifications for a given project record Id and status.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record.</param>
    /// <param name="status">The status of the project modification.</param>
    /// <returns>An asynchronous operation that returns all saved project modifications with the specified status.</returns>
    [Get("/projectmodifications/{projectRecordId}/{status}")]
    public Task<ApiResponse<IEnumerable<ProjectModificationResponse>>> GetModificationsByStatus(string projectRecordId, string status);

    /// <summary>
    /// Creates a new project modification.
    /// </summary>
    /// <param name="projectModificationRequest">The request object containing details for the new project modification.</param>
    /// <returns>An asynchronous operation that returns the newly created project modification.</returns>
    [Post("/projectmodifications")]
    public Task<ApiResponse<ProjectModificationResponse>> CreateModification(ProjectModificationRequest projectModificationRequest);

    /// <summary>
    /// Creates a new project modification change.
    /// </summary>
    /// <param name="projectModificationChangeRequest">The request object containing details for the project modification change.</param>
    /// <returns>An asynchronous operation that returns the newly created project modification change.</returns>
    [Post("/projectmodifications/change")]
    public Task<ApiResponse<ProjectModificationChangeResponse>> CreateModificationChange(ProjectModificationChangeRequest projectModificationChangeRequest);

    /// <summary>
    /// Gets all the area of changes and specific area of changes for the modification.
    /// </summary>
    /// <returns>An asynchronous operation that returns all area of changes and specific area of changes.</returns>
    [Get("/projectmodifications/areaofchanges")]
    public Task<ApiResponse<IEnumerable<GetAreaOfChangesResponse>>> GetAreaOfChanges();
}