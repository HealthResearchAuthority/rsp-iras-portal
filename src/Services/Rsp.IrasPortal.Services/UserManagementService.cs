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

    public async Task<ServiceResponse<UsersResponse>> GetUsers(SearchUserRequest? searchQuery = null, int pageNumber = 1, int pageSize = 10, string? sortField = "GivenName", string? sortDirection = SortDirections.Ascending)
    {
        var apiResponse = await client.GetUsers(searchQuery, pageNumber, pageSize, sortField, sortDirection);

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

    public async Task<ServiceResponse<UserResponse>> GetUser(string? userId, string? email, string? identityProviderId = null)
    {
        var apiResponse = await client.GetUser(userId, email, identityProviderId);

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

    public async Task<ServiceResponse> HandlePostLoginActivities(PostLoginOperationRequest userClaims)
    {
        var response = await client.HandlePostLoginActivities(userClaims);

        return response.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateUserEmailAndPhoneNumber(User user, string email, string? telephoneNumber)
    {
        var updateNeeded = false;
        var updateRequest = user.Adapt<UpdateUserRequest>();

        // check if email from govUk is different than one stored in our DB
        if (!user.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase))
        {
            updateRequest.Email = email;
            updateNeeded = true;
        }

        // check if telephone from govUk is different than one stored in our DB
        if (!string.IsNullOrEmpty(telephoneNumber) &&
            user.Telephone != telephoneNumber)
        {
            updateRequest.Telephone = telephoneNumber;
            updateNeeded = true;
        }

        // update the user record only if there are changes
        if (updateNeeded)
        {
            updateRequest.OriginalEmail = updateRequest.Email!;
            var response = await UpdateUser(updateRequest);
            return response;
        }

        return new ServiceResponse
        {
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
    }

    public async Task<ServiceResponse> UpdateUserIdentityProviderId(User user, string identityProviderId)
    {
        if (string.IsNullOrEmpty(identityProviderId))
        {
            return new ServiceResponse
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Error = "identityProviderId parameter cannot be empty string."
            };
        }
        var updateRequest = user.Adapt<UpdateUserRequest>();
        updateRequest.IdentityProviderId = identityProviderId;
        updateRequest.OriginalEmail = user.Email;

        return await UpdateUser(updateRequest);
    }
}