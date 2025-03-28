using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class ReviewBodyService(IReviewBodyServiceClient client) : IReviewBodyService
{
    public async Task<ServiceResponse<IEnumerable<ReviewBodyDto>>> GetAllReviewBodies()
    {
        var apiResponse = await client.GetAllReviewBodies();

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<ReviewBodyDto>>> GetReviewBodyById(Guid id)
    {
        var apiResponse = await client.GetReviewBodyById(id);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> CreateReviewBody(ReviewBodyDto reviewBodyDto)
    {
        var apiResponse = await client.CreateReviewBody(reviewBodyDto);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateReviewBody(ReviewBodyDto reviewBodyDto)
    {
        var apiResponse = await client.UpdateReviewBody(reviewBodyDto);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> DisableReviewBody(Guid id)
    {
        var apiResponse = await client.DisableReviewBody(id);

        return apiResponse.ToServiceResponse();
    }


    public async Task<ServiceResponse> EnableReviewBody(Guid id)
    {
        var apiResponse = await client.EnableReviewBody(id);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ReviewBodyAuditTrailResponse>> ReviewBodyAuditTrail(Guid id, int skip, int take)
    {
        var apiResponse = await client.GetReviewBodyAuditTrail(id, skip, take);

        return apiResponse.ToServiceResponse();
    }
}