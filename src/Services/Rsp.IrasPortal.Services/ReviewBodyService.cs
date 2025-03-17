using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class ReviewBodyService(IReviewBodyServiceClient client) : IReviewBodyService
{
    public async Task<ServiceResponse<IEnumerable<ReviewBodyDto>>> GetReviewBodies()
    {
        var apiResponse = await client.GetReviewBodies();

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<ReviewBodyDto>>> GetReviewBodies(Guid id)
    {
        var apiResponse = await client.GetReviewBodies(id);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> CreateReviewBody(ReviewBodyDto reviewBodyDto)
    {
        var apiResponse = await client.CreateReviewBody(reviewBodyDto);

        return apiResponse.ToServiceResponse();
    }
}