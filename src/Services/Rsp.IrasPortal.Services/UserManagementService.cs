﻿using Mapster;
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
}