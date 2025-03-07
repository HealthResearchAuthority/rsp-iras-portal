using Rsp.IrasPortal.Application.DTOs;
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

    public async Task<ServiceResponse> CreateUser(string firstName, string lastName, string email)
    {
        var apiResponse = await client.CreateUser
        (
            new User(null, firstName, lastName, email)
        );

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateUser(string originalEmail, string firstName, string lastName, string email)
    {
        var apiResponse = await client.UpdateUser
        (
            originalEmail,
            new User(null, firstName, lastName, email)
        );

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

    public async Task<ServiceResponse> UpdateRoles(string email, string rolesToRemove, string rolesToAdd)
    {
        var apiRemoveUsersResponse = await client.RemoveUsersFromRoles(email, rolesToRemove);

        if (!apiRemoveUsersResponse.IsSuccessStatusCode)
        {
            return apiRemoveUsersResponse.ToServiceResponse();
        }

        var apiAddUserToRolesRespoinse = await client.AddUserToRoles(email, rolesToAdd);

        return apiAddUserToRolesRespoinse.ToServiceResponse();
    }
}