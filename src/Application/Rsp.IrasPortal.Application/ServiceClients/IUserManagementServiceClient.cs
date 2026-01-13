using Refit;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests.UserManagement;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Domain.Identity;

namespace Rsp.Portal.Application.ServiceClients;

public interface IUserManagementServiceClient
{
    /// <summary>
    /// Gets all the roles in the database
    /// </summary>
    /// <returns>List of roles</returns>
    [Get("/roles")]
    public Task<ApiResponse<RolesResponse>> GetRoles(int pageIndex = 1, int pageSize = 100);

    /// <summary>
    /// Creates a new role in the database
    /// </summary>
    [Post("/roles")]
    public Task<IApiResponse> CreateRole(string roleName);

    /// <summary>
    /// Updates a role in the database
    /// </summary>
    [Put("/roles")]
    public Task<IApiResponse> UpdateRole(string roleName, string newName);

    /// <summary>
    /// Deletes a role from the database
    /// </summary>
    [Delete("/roles")]
    public Task<IApiResponse> DeleteRole(string roleName);

    /// <summary>
    /// Gets all the users in the database
    /// </summary>
    /// <returns>List of users</returns>
    [Post("/users/all")]
    public Task<ApiResponse<UsersResponse>> GetUsers(SearchUserRequest? searchQuery = null, int pageIndex = 1, int pageSize = 20, string? sortField = "GivenName", string? sortDirection = SortDirections.Descending);

    /// <summary>
    /// Gets users by their ids database
    /// </summary>
    /// <returns>List of users</returns>
    [Post("/users/by-ids")]
    public Task<IApiResponse<UsersResponse>> GetUsersById([Body] IEnumerable<string> ids, string? searchQuery = null, int pageIndex = 1, int pageSize = 10);

    /// <summary>
    /// Gets a user by id or email
    /// </summary>
    /// <param name="id">Id of the user</param>
    /// <param name="email">Email of the user</param>
    /// <returns>List of users</returns>
    [Get("/users")]
    public Task<ApiResponse<UserResponse>> GetUser(string? id, string? email, string? identityProviderId);

    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="user">Request Body</param>
    [Post("/users")]
    public Task<IApiResponse> CreateUser([Body] User user);

    /// <summary>
    /// Deletes a user by id or email
    /// </summary>
    /// <param name="email">Email of the user</param>
    /// <returns>List of users</returns>
    [Put("/users")]
    public Task<IApiResponse> UpdateUser(string email, [Body] User user);

    /// <summary>
    /// Deletes a user by id or email
    /// </summary>
    /// <param name="id">Id of the user</param>
    /// <param name="email">Email of the user</param>
    /// <returns>List of users</returns>
    [Delete("/users")]
    public Task<IApiResponse> DeleteUser(string id, string email);

    /// <summary>
    /// Gets all the users in a role
    /// <paramref name="roleName">Role Name</paramref>
    /// </summary>
    /// <returns>List of users</returns>
    [Get("/users/role")]
    public Task<ApiResponse<UsersResponse>> GetUsersInRole(string roleName);

    /// <summary>
    /// Adds a users to a role or multiple roles
    /// <paramref name="roles">Comma separated list of roles</paramref>
    /// </summary>
    [Post("/users/roles")]
    public Task<IApiResponse> AddUserToRoles(string email, string roles);

    /// <summary>
    /// Removes a users from a role or multiple roles
    /// <paramref name="roles">Comma separated list of roles</paramref>
    /// </summary>
    [Delete("/users/roles")]
    public Task<IApiResponse> RemoveUsersFromRoles(string email, string roles);

    /// <summary>
    /// Gets a user by id or email
    /// </summary>
    /// <param name="email">Email of the user</param>
    /// <returns>List of users</returns>
    [Get("/users/audit")]
    public Task<ApiResponse<UserAuditTrailResponse>> GetUserAuditTrail(string userId);

    /// <summary>
    /// Searches users by their name or email
    /// </summary>
    /// <param name="searchQuery">Search query</param>
    /// <returns>List of users</returns>
    [Post("/users/search")]
    public Task<IApiResponse<UsersResponse>> SearchUsers(string searchQuery, [Body] IEnumerable<string>? userIdsToIgnore = null, int pageIndex = 1, int pageSize = 10);

    /// <summary>
    /// Add claims for a user
    /// </summary>
    [Post("/users/claims")]
    public Task<IApiResponse> AddUserClaims([Body] UserClaimsRequest claimsRequest);

    /// <summary>
    /// Remove user claims
    /// </summary>
    [Delete("/users/claims")]
    public Task<IApiResponse> RemoveUserClaims([Body] UserClaimsRequest claimsRequest);

    /// <summary>
    /// Get claims for a user
    /// </summary>
    [Get("/users/claims")]
    public Task<IApiResponse<IEnumerable<UserClaimDto>>> GetUserClaims(string? id, string? email);
}