using System.Security.Claims;
using Rsp.Portal.Application.AccessControl;
using Rsp.Portal.Application.Constants;

namespace Rsp.Portal.Application.Extensions;

/// <summary>
/// Extension methods for permission-based authorization in controllers
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    public static bool HasPermission
    (
        this ClaimsPrincipal user,
        string permission
    )
    {
        if (user.IsInRole(Roles.SystemAdministrator))
        {
            return true; // System Administrators have all permissions
        }

        // Extract permissions for the user
        var permissions = user.Claims
            .Where(c => c.Type == CustomClaimTypes.Permissions)
            .Select(c => c.Value)
            .ToList();

        if (permissions.Count == 0)
        {
            return false;
        }

        // Check if user has the required permission
        return permissions.Contains(permission);
    }

    /// <summary>
    /// Checks if the current user can access a record with a specific status
    /// </summary>
    public static bool CanAccessRecordStatus
    (
        this ClaimsPrincipal user,
        string entityType,
        string status
    )
    {
        if (user.IsInRole(Roles.SystemAdministrator))
        {
            return true; // System Administrators have all permissions
        }

        // Extract permissions for the user
        var allowedStatuses = user.Claims
            .Where(c => c.Type == $"allowed_statuses/{entityType}")
            .Select(c => c.Value)
            .ToList();

        if (allowedStatuses.Count == 0)
        {
            return false;
        }

        return allowedStatuses.Contains(status);
    }

    /// <summary>
    /// Gets all permissions for the current user
    /// </summary>
    public static List<string> GetUserPermissions(this ClaimsPrincipal user)
    {
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return RolePermissions.GetPermissionsForRoles(userRoles);
    }

    /// <summary>
    /// Gets allowed statuses for the current user for a specific entity type
    /// </summary>
    public static List<string> GetAllowedStatuses(this ClaimsPrincipal user, string entityType)
    {
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return RoleStatusPermissions.GetAllowedStatusesForRoles(userRoles, entityType);
    }
}