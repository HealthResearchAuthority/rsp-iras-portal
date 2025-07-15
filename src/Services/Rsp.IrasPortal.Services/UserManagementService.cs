using System.Net;
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

    public async Task<ServiceResponse<UsersResponse>> GetUsers(SearchUserRequest searchQuery = null, int pageNumber = 1, int pageSize = 10)
    {
        var apiResponse = await client.GetUsers(searchQuery, pageNumber, pageSize);

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
        // Get all user claims
        var allUserClaims = await client.GetUserClaims(null, userEmail);

        var currentAccessClaims = allUserClaims.Content?
            .Where(x => x.Type == UserClaimTypes.AccessRequired)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

        var desiredAccessClaims = accessRequired?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

        // Determine claims to remove (present now but not required)
        var claimsToRemove = currentAccessClaims.Except(desiredAccessClaims).ToList();

        // Determine claims to add (required but not currently present)
        var claimsToAdd = desiredAccessClaims.Except(currentAccessClaims).ToList();

        // Remove only claims that are not required anymore
        if (claimsToRemove.Any())
        {
            var deleteClaimsRequest = new UserClaimsRequest
            {
                Email = userEmail,
                Claims = claimsToRemove
                    .Select(value => new KeyValuePair<string, string>(UserClaimTypes.AccessRequired, value))
                    .ToList()
            };

            await client.RemoveUserClaims(deleteClaimsRequest);
        }

        // Add only new claims that the user doesn't already have
        if (claimsToAdd.Any())
        {
            var addClaimsRequest = new UserClaimsRequest
            {
                Email = userEmail,
                Claims = claimsToAdd
                    .Select(value => new KeyValuePair<string, string>(UserClaimTypes.AccessRequired, value))
                    .ToList()
            };

            var apiResponse = await client.AddUserClaims(addClaimsRequest);
            return apiResponse.ToServiceResponse();
        }

        // No changes needed
        return new ServiceResponse
        {
            ReasonPhrase = "No changes needed",
            StatusCode = HttpStatusCode.OK
        };
    }

    public async Task<ServiceResponse> UpdateLastLogin(string email)
    {
        var getUserResponse = await GetUser(null, email);

        if (getUserResponse.IsSuccessStatusCode)
        {
            var updateUserRequest = getUserResponse.Content!.User.Adapt<UpdateUserRequest>();
            updateUserRequest.CurrentLogin = DateTime.UtcNow;
            updateUserRequest.OriginalEmail = email;

            var updateUserResponse = await UpdateUser(updateUserRequest);

            return new ServiceResponse
            {
                StatusCode = updateUserResponse.StatusCode
            };
        }

        return new ServiceResponse
        {
            StatusCode = getUserResponse.StatusCode
        };
    }
}