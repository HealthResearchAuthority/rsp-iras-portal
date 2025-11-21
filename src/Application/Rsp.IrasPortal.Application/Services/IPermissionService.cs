using System.Security.Claims;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Service interface for checking user permissions
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if the user has a specific permission
    /// </summary>
    bool HasPermission(ClaimsPrincipal user, string permission);

    /// <summary>
    /// Gets all permissions for the current user based on their roles
    /// </summary>
    List<string> GetUserPermissions(ClaimsPrincipal user);

    /// <summary>
    /// Checks if the user can access a record based on its status
    /// </summary>
    bool CanAccessRecordStatus(ClaimsPrincipal user, string entityType, string status);

    /// <summary>
    /// Gets all allowed statuses for a user based on entity type
    /// </summary>
    List<string> GetAllowedStatuses(ClaimsPrincipal user, string entityType);
}