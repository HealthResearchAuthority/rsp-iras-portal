using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.Extensions;

/// <summary>
/// Extension methods for permission-based authorization in controllers
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    public static bool HasPermission
    (
        this Controller controller,
        IPermissionService permissionService,
        string permission
    )
    {
        return permissionService.HasPermission(controller.User, permission);
    }

    /// <summary>
    /// Checks if the current user can access a record with a specific status
    /// </summary>
    public static bool CanAccessRecordStatus
    (
        this Controller controller,
        IPermissionService permissionService,
        string entityType,
        string status
    )
    {
        return permissionService.CanAccessRecordStatus(controller.User, entityType, status);
    }

    /// <summary>
    /// Returns Forbid result if user doesn't have the required permission
    /// </summary>
    public static IActionResult? ForbidIfNoPermission
    (
        this Controller controller,
        IPermissionService permissionService,
        string permission
    )
    {
        if (!permissionService.HasPermission(controller.User, permission))
        {
            return controller.Forbid();
        }

        return null;
    }

    /// <summary>
    /// Checks if the current user has a specific permission (for ClaimsPrincipal)
    /// </summary>
    public static bool HasPermission(this ClaimsPrincipal user, IPermissionService permissionService, string permission)
    {
        return permissionService.HasPermission(user, permission);
    }

    /// <summary>
    /// Returns Forbid result if user cannot access the record status
    /// </summary>
    public static IActionResult? ForbidIfCannotAccessStatus
    (
        this Controller controller,
        IPermissionService permissionService,
        string entityType,
        string status
    )
    {
        if (!permissionService.CanAccessRecordStatus(controller.User, entityType, status))
        {
            return controller.Forbid();
        }

        return null;
    }

    /// <summary>
    /// Gets all permissions for the current user
    /// </summary>
    public static List<string> GetUserPermissions(this Controller controller, IPermissionService permissionService)
    {
        return permissionService.GetUserPermissions(controller.User);
    }

    /// <summary>
    /// Gets allowed statuses for the current user for a specific entity type
    /// </summary>
    public static List<string> GetAllowedStatuses
    (
        this Controller controller,
        IPermissionService permissionService,
        string entityType
    )
    {
        return permissionService.GetAllowedStatuses(controller.User, entityType);
    }

    /// <summary>
    /// Checks if the current user can access a record with a specific status (for ClaimsPrincipal)
    /// </summary>
    public static bool CanAccessRecordStatus
    (
        this ClaimsPrincipal user,
        IPermissionService permissionService,
        string entityType,
        string status
    )
    {
        return permissionService.CanAccessRecordStatus(user, entityType, status);
    }
}