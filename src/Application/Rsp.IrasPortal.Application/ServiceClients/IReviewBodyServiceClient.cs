using Refit;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
///     Interface to interact with Applications microservice
/// </summary>
public interface IReviewBodyServiceClient
{
    /// <summary>
    /// Gets all review bodies
    /// </summary>
    [Post("/reviewbody/all")]
    public Task<IApiResponse<AllReviewBodiesResponse>> GetAllReviewBodies(int pageNumber = 1, int pageSize = 20, string? sortField = nameof(ReviewBodyDto.RegulatoryBodyName), string? sortDirection = SortDirections.Ascending, ReviewBodySearchRequest? searchQuery = null);

    /// <summary>
    /// Gets all review bodies
    /// </summary>
    [Post("/reviewbody/allactive")]
    public Task<IApiResponse<AllReviewBodiesResponse>> GetAllActiveReviewBodies(string? sortField = nameof(ReviewBodyDto.RegulatoryBodyName), string? sortDirection = SortDirections.Ascending);

    /// <summary>
    /// Gets review bodies by Id
    /// </summary>
    [Get("/reviewbody/{id}")]
    public Task<IApiResponse<ReviewBodyDto>> GetReviewBodyById(Guid id);

    /// <summary>
    ///     Creates a new review body in the database
    /// </summary>
    [Post("/reviewbody/create")]
    public Task<IApiResponse<ReviewBodyDto>> CreateReviewBody(ReviewBodyDto reviewBodyDto);

    /// <summary>
    ///     Updates a review body in the database
    /// </summary>
    [Post("/reviewbody/update")]
    public Task<IApiResponse<ReviewBodyDto>> UpdateReviewBody(ReviewBodyDto reviewBodyDto);

    /// <summary>
    /// Gets review bodies by Id
    /// </summary>
    [Put("/reviewbody/disable/{id}")]
    public Task<IApiResponse<ReviewBodyDto>> DisableReviewBody(Guid id);

    /// <summary>
    /// Gets review bodies by Id
    /// </summary>
    [Put("/reviewbody/enable/{id}")]
    public Task<IApiResponse<ReviewBodyDto>> EnableReviewBody(Guid id);

    /// <summary>
    /// Gets review bodies by Id
    /// </summary>
    [Get("/reviewbody/audittrail")]
    public Task<IApiResponse<ReviewBodyAuditTrailResponse>> GetReviewBodyAuditTrail(Guid id, int skip, int take);

    [Post("/reviewbody/adduser")]
    public Task<IApiResponse<ReviewBodyUserDto>> AddUserToReviewBody([Body] ReviewBodyUserDto reviewBodyUser);

    [Post("/reviewbody/removeuser")]
    public Task<IApiResponse<ReviewBodyUserDto>> RemoveUserFromReviewBody(Guid reviewBodyId, Guid userId);

    /// <summary>
    /// Gets review bodies by Id
    /// </summary>
    [Get("/reviewbody/allbyuser/{id}")]
    public Task<IApiResponse<List<ReviewBodyUserDto>>> GetUserReviewBodies(Guid id);

}