using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface IUserManagementService
{
    Task<ServiceResponse> CreateRole(string roleName);

    Task<ServiceResponse> DeleteRole(string roleName);

    Task<ServiceResponse<RolesResponse>> GetRoles();

    Task<ServiceResponse> UpdateRole(string originalName, string roleName);

    Task<ServiceResponse<UsersResponse>> GetUsers();

    Task<ServiceResponse<UserResponse>> GetUser(string? userId, string? email);

    Task<ServiceResponse> CreateUser(string firstName, string lastName, string email);

    Task<ServiceResponse> UpdateUser(string originalEmail, string firstName, string lastName, string email);

    Task<ServiceResponse> DeleteUser(string userId, string email);

    Task<ServiceResponse> UpdateRoles(string email, string rolesToRemove, string rolesToAdd);
}