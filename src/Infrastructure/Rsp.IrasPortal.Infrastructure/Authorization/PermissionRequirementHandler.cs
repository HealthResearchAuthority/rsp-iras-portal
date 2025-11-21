using Microsoft.AspNetCore.Authorization;

namespace Rsp.IrasPortal.Infrastructure.Authorization;

/// <summary>
/// Handler that checks if a user has the required permission based on their roles
/// </summary>
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // succeed if user is in Admin role
        //if (context.User.IsInRole(Roles.SystemAdministrator))
        //{
        //    context.Succeed(requirement);
        //    return Task.CompletedTask;
        //}

        // Extract permissions for the user
        var permissions = context.User.Claims
            .Where(c => c.Type == "permissions")
            .Select(c => c.Value)
            .ToList();

        if (permissions.Count == 0)
        {
            return Task.CompletedTask;
        }

        // Check if user has the required permission
        var hasPermission = permissions.Contains(requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}