using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests.UserManagement;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Domain.Identity;
using Rsp.Logging.Interceptors;

namespace Rsp.Portal.Application.Services;

/// <summary>
/// IUserManagementService interface. Marked as IInterceptable to enable the start/end logging for
/// all methods.
/// </summary>
public interface IUserManagementService : IInterceptable
{
    Task<ServiceResponse> CreateRole(string roleName);

    Task<ServiceResponse> DeleteRole(string roleName);

    Task<ServiceResponse<RolesResponse>> GetRoles(int pageNumber = 1, int pageSize = 10);

    Task<ServiceResponse> UpdateRole(string originalName, string roleName);

    Task<ServiceResponse<UsersResponse>> GetUsers(SearchUserRequest? searchQuery = null, int pageNumber = 1, int pageSize = 10, string? sortField = "GivenName", string? sortDirection = SortDirections.Ascending);

    Task<ServiceResponse<UsersResponse>> SearchUsers(string searchQuery, IEnumerable<string>? userIdsToIgnore = null, int pageNumber = 1, int pageSize = 10);

    Task<ServiceResponse<UsersResponse>> GetUsersByIds(IEnumerable<string> ids, string? searchQuery = null, int pageNumber = 1, int pageSize = 10);

    Task<ServiceResponse<UserResponse>> GetUser(string? userId, string? email, string? identityProviderId = null);

    Task<ServiceResponse> CreateUser(CreateUserRequest request);

    Task<ServiceResponse> UpdateUser(UpdateUserRequest request);

    Task<ServiceResponse> DeleteUser(string userId, string email);

    Task<ServiceResponse> UpdateRoles(string email, string? rolesToRemove, string rolesToAdd);

    Task<ServiceResponse<UserAuditTrailResponse>> GetUserAuditTrail(string userId);

    Task<ServiceResponse> UpdateLastLogin(string email);

    Task<ServiceResponse> UpdateUserEmailAndPhoneNumber(User user, string email, string? telephoneNumber);

    Task<ServiceResponse> UpdateUserIdentityProviderId(User user, string identityProviderId);
}