using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
///     IReviewBodyService interface. Marked as IInterceptable to enable
///     the start/end logging for all methods.
/// </summary>
public interface IReviewBodyService : IInterceptable
{
    Task<ServiceResponse<IEnumerable<ReviewBodyDto>>> GetAllReviewBodies();
    Task<ServiceResponse<IEnumerable<ReviewBodyDto>>> GetReviewBodyById(Guid id);
    Task<ServiceResponse> CreateReviewBody(ReviewBodyDto reviewBodyDto);
    Task<ServiceResponse> UpdateReviewBody(ReviewBodyDto reviewBodyDto);
    Task<ServiceResponse> DisableReviewBody(Guid id);
    Task<ServiceResponse> EnableReviewBody(Guid id);
}