using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
///     IReviewBodyService interface. Marked as IInterceptable to enable
///     the start/end logging for all methods.
/// </summary>
public interface IReviewBodyService : IInterceptable
{
    Task<ServiceResponse<AllReviewBodiesResponse>> GetAllReviewBodies(int pageNumber = 1, int pageSize = 20, string? searchQuery = null);

    Task<ServiceResponse<ReviewBodyDto>> GetReviewBodyById(Guid id);

    Task<ServiceResponse> CreateReviewBody(ReviewBodyDto reviewBodyDto);

    Task<ServiceResponse> UpdateReviewBody(ReviewBodyDto reviewBodyDto);

    Task<ServiceResponse> DisableReviewBody(Guid id);

    Task<ServiceResponse<ReviewBodyAuditTrailResponse>> ReviewBodyAuditTrail(Guid id, int skip, int take);

    Task<ServiceResponse> EnableReviewBody(Guid id);

    Task<ServiceResponse<ReviewBodyUserDto>> AddUserToReviewBody(ReviewBodyUserDto reviewBodyUser);

    Task<ServiceResponse<ReviewBodyUserDto>> RemoveUserFromReviewBody(Guid reviewBodyId, Guid userId);
}