using Refit;
using Rsp.IrasPortal.Application.DTOs;
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
    [Get("/reviewbody")]
    public Task<IApiResponse<IEnumerable<ReviewBodyDto>>> GetAllReviewBodies();

    /// <summary>
    /// Gets review bodies by Id
    /// </summary>
    [Get("/reviewbody/{id}")]
    public Task<IApiResponse<IEnumerable<ReviewBodyDto>>> GetReviewBodyById(Guid id);

    /// <summary>
    ///     Creates a new review body in the database
    /// </summary>
    [Post("/reviewbody/create")]
    public Task<IApiResponse> CreateReviewBody(ReviewBodyDto reviewBodyDto);

    /// <summary>
    ///     Updates a review body in the database
    /// </summary>
    [Post("/reviewbody/update")]
    public Task<IApiResponse> UpdateReviewBody(ReviewBodyDto reviewBodyDto);

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
}