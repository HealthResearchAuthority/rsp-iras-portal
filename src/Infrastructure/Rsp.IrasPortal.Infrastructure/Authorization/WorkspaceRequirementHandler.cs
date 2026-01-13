using Microsoft.AspNetCore.Authorization;
using Rsp.Portal.Application.AccessControl;
using Rsp.Portal.Application.Constants;

namespace Rsp.Portal.Infrastructure.Authorization;

/// <summary>
/// Handler that checks if a user has the required permission based on their roles
/// </summary>
/// <remarks>
/// This handler consults the <see cref="WorkspaceRolesMatrix"/> to determine which roles
/// are allowed to access a given workspace. If the current user has at least one of
/// the allowed roles, the requirement is marked as succeeded.
/// </remarks>
public class WorkspaceRequirementHandler : AuthorizationHandler<WorkspaceRequirement>
{
    /// <summary>
    /// Called by the authorization system to evaluate the requirement for the current user.
    /// </summary>
    /// <param name="context">Authorization context containing the user principal and resources.</param>
    /// <param name="requirement">The workspace requirement to evaluate.</param>
    /// <returns>A completed <see cref="Task"/>. The context is updated if the requirement succeeds.</returns>
    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context,
        WorkspaceRequirement requirement
    )
    {
        // succeed if user is in Admin role
        if (context.User.IsInRole(Roles.SystemAdministrator))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.FindFirst(CustomClaimTypes.UserStatus)?.Value is IrasUserStatus.Disabled)
        {
            // Disabled users cannot access any workspace.
            return Task.CompletedTask;
        }

        // Try to lookup the allowed roles for the requested workspace.
        // If the workspace is not present in the matrix, no roles are allowed.
        if (!WorkspaceRolesMatrix.WorkspaceRoles.TryGetValue(requirement.Workspace, out var allowedRoles))
        {
            // No mapping found -> leave requirement unresolved (authorization will not succeed here).
            return Task.CompletedTask;
        }

        // The workspace is accessible if the user has at least one role that appears in the allowedRoles list.
        // Check for any intersection between the user's roles and the allowed roles.
        if (allowedRoles.Any(context.User.IsInRole))
        {
            // Mark the requirement as succeeded so that the authorization system allows the access.
            context.Succeed(requirement);
        }

        // Complete the asynchronous operation. If context.Succeed was not called, the requirement is considered not satisfied.
        return Task.CompletedTask;
    }
}