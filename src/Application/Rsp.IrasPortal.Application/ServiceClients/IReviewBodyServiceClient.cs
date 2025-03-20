using Refit;
using Rsp.IrasPortal.Application.DTOs;

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
}