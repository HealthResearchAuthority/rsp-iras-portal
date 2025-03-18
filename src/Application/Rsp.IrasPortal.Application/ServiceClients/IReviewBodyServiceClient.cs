using Refit;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
///     Interface to interact with Applications microservice
/// </summary>
public interface IReviewBodyServiceClient
{
    [Get("/reviewbody")]
    public Task<IApiResponse<IEnumerable<ReviewBodyDto>>> GetAllReviewBodies();

    [Get("/reviewbody")]
    public Task<IApiResponse<IEnumerable<ReviewBodyDto>>> GetReviewBodyById(Guid id);

    /// <summary>
    ///     Creates a new review body in the database
    /// </summary>
    [Post("/reviewbody/create")]
    public Task<IApiResponse> CreateReviewBody(ReviewBodyDto reviewBodyDto);
}