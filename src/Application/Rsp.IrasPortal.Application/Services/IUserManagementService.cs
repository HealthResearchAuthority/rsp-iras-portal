using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// IUserManagementService interface. Marked as IInterceptable to enable
/// the start/end logging for all methods.
/// </summary>
public interface IUserManagementService : IInterceptable
{
    Task<ServiceResponse> CreateRole(string roleName);

    Task<ServiceResponse> DeleteRole(string roleName);

    Task<ServiceResponse<RolesResponse>> GetRoles(int pageNumber = 1, int pageSize = 10);

    Task<ServiceResponse> UpdateRole(string originalName, string roleName);

    Task<ServiceResponse<UsersResponse>> GetUsers(int pageNumber = 1, int pageSize = 10);

    Task<ServiceResponse<UserResponse>> GetUser(string? userId, string? email);

    Task<ServiceResponse> CreateUser(CreateUserRequest request);

    Task<ServiceResponse> UpdateUser(UpdateUserRequest request);

    Task<ServiceResponse> DeleteUser(string userId, string email);

    Task<ServiceResponse> UpdateRoles(string email, string? rolesToRemove, string rolesToAdd);
}