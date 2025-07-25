﻿using Refit;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
/// Interface to interact with Applications microservice
/// </summary>
public interface IApplicationsServiceClient
{
    /// <summary>
    /// Gets the saved application by Id
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    [Get("/applications/{applicationId}")]
    public Task<ApiResponse<IrasApplicationResponse>> GetProjectRecord(string applicationId);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    [Get("/applications")]
    public Task<ApiResponse<IEnumerable<IrasApplicationResponse>>> GetApplications();

    /// <summary>
    /// Gets the saved application by Id and status
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <param name="status">Application Status</param>
    /// <returns>An asynchronous operation that returns a saved application.</returns>
    [Get("/applications/{applicationId}/{status}")]
    public Task<ApiResponse<IrasApplicationResponse>> GetApplicationByStatus(string applicationId, string status);

    /// <summary>
    /// Gets all the saved applications
    /// </summary>
    /// <param name="status">Status of the application</param>
    /// <returns>An asynchronous operation that returns all the saved application.</returns>
    [Get("/applications/status")]
    public Task<ApiResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByStatus(string status);

    /// <summary>
    /// Gets all the saved applications by respondent
    /// </summary>
    /// <param name="respondentId">Respondent Id associated with the application</param>
    /// <returns>An asynchronous operation that returns all the saved applications for a given respondent.</returns>
    [Get("/applications/respondent")]
    public Task<ApiResponse<IEnumerable<IrasApplicationResponse>>> GetApplicationsByRespondent(string respondentId);

    /// <summary>
    /// Gets all the saved applications by respondent
    /// </summary>
    /// <param name="respondentId">Respondent Id associated with the application</param>
    /// <returns>An asynchronous operation that returns all the saved applications for a given respondent.</returns>
    [Get("/applications/respondent/paginated")]
    public Task<ApiResponse<PaginatedResponse<IrasApplicationResponse>>> GetPaginatedApplicationsByRespondent(string respondentId, string? searchQuery = null, int pageIndex = 0, int pageSize = 0);

    /// <summary>
    /// Creates a new application
    /// </summary>
    /// <returns>An asynchronous operation that returns the newly created application.</returns>
    [Post("/applications")]
    public Task<ApiResponse<IrasApplicationResponse>> CreateApplication(IrasApplicationRequest irasApplication);

    /// <summary>
    /// Updates the saved application by Id
    /// </summary>
    /// <returns>An asynchronous operation that updates the existing application.</returns>
    [Put("/applications")]
    public Task<ApiResponse<IrasApplicationResponse>> UpdateApplication(IrasApplicationRequest irasApplication);

    [Get("/applications/modifications")]
    public Task<ApiResponse<GetModificationsResponse>> GetModifications
    (
        [Body] ModificationSearchRequest searchQuery,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsDto.ModificationId),
        string sortDirection = SortDirections.Descending
    );
}