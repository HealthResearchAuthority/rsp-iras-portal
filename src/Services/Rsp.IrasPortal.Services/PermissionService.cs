using System.Security.Claims;
using Rsp.IrasPortal.Application.AccessControl;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Services;

/// <summary>
/// Default implementation of permission service
/// </summary>
public class PermissionService : IPermissionService
{
    public bool HasPermission(ClaimsPrincipal user, string permission)
    {
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return RolePermissions.HasPermission(userRoles, permission);
    }

    public List<string> GetUserPermissions(ClaimsPrincipal user)
    {
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return RolePermissions.GetPermissionsForRoles(userRoles);
    }

    public bool CanAccessRecordStatus(ClaimsPrincipal user, string entityType, string status)
    {
        var allowedStatuses = GetAllowedStatuses(user, entityType);
        return allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    public List<string> GetAllowedStatuses(ClaimsPrincipal user, string entityType)
    {
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return RoleStatusPermissions.GetAllowedStatusesForRoles(userRoles, entityType);
    }
}