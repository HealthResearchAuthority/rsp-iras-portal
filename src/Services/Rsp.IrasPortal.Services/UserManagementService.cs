using Mapster;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class UserManagementService(IUserManagementServiceClient client) : IUserManagementService
{
    public async Task<ServiceResponse> CreateRole(string roleName)
    {
        var apiResponse = await client.CreateRole(roleName);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> DeleteRole(string roleName)
    {
        var apiResponse = await client.DeleteRole(roleName);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<RolesResponse>> GetRoles(int pageNumber = 1, int pageSize = 10)
    {
        var apiResponse = await client.GetRoles(pageNumber, pageSize);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateRole(string originalName, string roleName)
    {
        var apiResponse = await client.UpdateRole(originalName, roleName);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<UsersResponse>> GetUsers(int pageNumber = 1, int pageSize = 10)
    {
        var apiResponse = await client.GetUsers(pageNumber, pageSize);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<UsersResponse>> SearchUsers(string searchQuery, IEnumerable<string>? userIdsToIgnore = null, int pageNumber = 1, int pageSize = 10)
    {
        var apiResponse = await client.SearchUsers(searchQuery, userIdsToIgnore, pageNumber, pageSize);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<UsersResponse>> GetUsersByIds(IEnumerable<string> ids, string? searchQuery = null, int pageNumber = 1, int pageSize = 10)
    {
        var apiResponse = await client.GetUsersById(ids, searchQuery, pageNumber, pageSize);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> CreateUser(CreateUserRequest request)
    {
        var user = request.Adapt<User>();
        var apiResponse = await client.CreateUser(user);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateUser(UpdateUserRequest request)
    {
        var user = request.Adapt<User>();

        var apiResponse = await client.UpdateUser(request.OriginalEmail, user);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<UserResponse>> GetUser(string? userId, string? email)
    {
        var apiResponse = await client.GetUser(userId, email);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> DeleteUser(string userId, string email)
    {
        var apiResponse = await client.DeleteUser(userId, email);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateRoles(string email, string? rolesToRemove, string rolesToAdd)
    {
        var rolesToRemoveSet = rolesToRemove?.Split(',').Select(r => r.Trim()).ToHashSet() ?? [];
        var rolesToAddSet = rolesToAdd.Split(',').Select(r => r.Trim()).ToHashSet();

        // Remove common roles
        var commonRoles = rolesToRemoveSet.Intersect(rolesToAddSet).ToList();
        rolesToRemoveSet.ExceptWith(commonRoles);
        rolesToAddSet.ExceptWith(commonRoles);

        // Convert back to comma-separated strings
        var filteredRolesToRemove = string.Join(",", rolesToRemoveSet);
        var filteredRolesToAdd = string.Join(",", rolesToAddSet);

        if (!string.IsNullOrEmpty(filteredRolesToRemove))
        {
            var apiRemoveUsersResponse = await client.RemoveUsersFromRoles(email, filteredRolesToRemove);

            if (!apiRemoveUsersResponse.IsSuccessStatusCode)
            {
                return apiRemoveUsersResponse.ToServiceResponse();
            }
        }

        if (!string.IsNullOrEmpty(filteredRolesToAdd))
        {
            var apiAddUserToRolesResponse = await client.AddUserToRoles(email, filteredRolesToAdd);
            return apiAddUserToRolesResponse.ToServiceResponse();
        }

        return new ServiceResponse { StatusCode = System.Net.HttpStatusCode.OK };
    }

    public async Task<ServiceResponse<UserAuditTrailResponse>> GetUserAuditTrail(string userId)
    {
        var apiResponse = await client.GetUserAuditTrail(userId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateUserAccess(string userEmail, IEnumerable<string> accessRequired)
    {
        // get all user claims
        var allUserClaims = await client.GetUserClaims(null, userEmail);

        // check if user has "access required" claims
        if (allUserClaims.Content != null && allUserClaims.Content.Any(x => x.Type == UserClaimTypes.AccessRequired))
        {
            var deleteClaimsRequest = new UserClaimsRequest
            {
                Email = userEmail,
                Claims = []
            };

            foreach (var claim in allUserClaims.Content.Where(x => x.Type == UserClaimTypes.AccessRequired))
            {
                deleteClaimsRequest.Claims.Add(new KeyValuePair<string, string>(claim.Type, claim.Value));
            }

            // remove all access claims for user to avoid duplicate entries
            await client.RemoveUserClaims(deleteClaimsRequest);
        }

        var request = new UserClaimsRequest
        {
            Email = userEmail,
            Claims = []
        };

        foreach (var item in accessRequired)
        {
            request.Claims.Add(new KeyValuePair<string, string>(UserClaimTypes.AccessRequired, item));
        }

        // add new updated user claims for access required
        var apiResponse = await client.AddUserClaims(request);

        return apiResponse.ToServiceResponse();
    }
}