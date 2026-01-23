using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services.Extensions;

namespace Rsp.Portal.Services;

public class ReviewBodyService(IReviewBodyServiceClient client) : IReviewBodyService
{
    public async Task<ServiceResponse<AllReviewBodiesResponse>> GetAllReviewBodies(ReviewBodySearchRequest? searchQuery = null,
        int pageNumber = 1, int pageSize = 20, string? sortField = nameof(ReviewBodyDto.RegulatoryBodyName), string? sortDirection = SortDirections.Ascending)
    {
        var apiResponse = await client.GetAllReviewBodies(pageNumber, pageSize, sortField, sortDirection, searchQuery);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<AllReviewBodiesResponse>> GetAllActiveReviewBodies(string? sortField = nameof(ReviewBodyDto.RegulatoryBodyName), string? sortDirection = SortDirections.Ascending)
    {
        var apiResponse = await client.GetAllActiveReviewBodies(sortField, sortDirection);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ReviewBodyDto>> GetReviewBodyById(Guid id)
    {
        var apiResponse = await client.GetReviewBodyById(id);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ReviewBodyDto>> CreateReviewBody(ReviewBodyDto reviewBodyDto)
    {
        var apiResponse = await client.CreateReviewBody(reviewBodyDto);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ReviewBodyDto>> UpdateReviewBody(ReviewBodyDto reviewBodyDto)
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

    public async Task<ServiceResponse<ReviewBodyUserDto>> AddUserToReviewBody(ReviewBodyUserDto reviewBodyUser)
    {
        var apiResponse = await client.AddUserToReviewBody(reviewBodyUser);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ReviewBodyUserDto>> RemoveUserFromReviewBody(Guid reviewBodyId, Guid userId)
    {
        var apiResponse = await client.RemoveUserFromReviewBody(reviewBodyId, userId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<List<ReviewBodyUserDto>>> GetUserReviewBodies(Guid userId)
    {
        var apiResponse = await client.GetUserReviewBodies(userId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<List<ReviewBodyUserDto>>> GetUserReviewBodiesByReviewBodyIds(List<Guid> reviewBodyIdsByReviewBodyIds)
    {
        var apiResponse = await client.GetUserReviewBodiesByIds(reviewBodyIdsByReviewBodyIds);

        return apiResponse.ToServiceResponse();
    }
}