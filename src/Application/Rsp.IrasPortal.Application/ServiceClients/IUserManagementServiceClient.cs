using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Domain.Identity;

namespace Rsp.IrasPortal.Infrastructure.HttpClients;

public interface IUserManagementServiceClient
{
    /// <summary>
    /// Gets all the roles in the database
    /// </summary>
    /// <returns>List of roles</returns>
    [Get("/roles")]
    public Task<ApiResponse<RolesResponse>> GetRoles(int pageIndex = 1, int pageSize = 10);

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
    [Get("/users/all")]
    public Task<ApiResponse<UsersResponse>> GetUsers(int pageIndex = 1, int pageSize = 10);

    /// <summary>
    /// Gets a user by id or email
    /// </summary>
    /// <param name="id">Id of the user</param>
    /// <param name="email">Email of the user</param>
    /// <returns>List of users</returns>
    [Get("/users")]
    public Task<ApiResponse<UserResponse>> GetUser(string? id, string? email);

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
}