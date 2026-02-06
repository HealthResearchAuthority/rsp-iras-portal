using Microsoft.AspNetCore.Authorization;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Extensions;

namespace Rsp.Portal.Infrastructure.Authorization;

/// <summary>
/// Handler that checks if a user has the required permission based on their roles
/// </summary>
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // succeed if user is in Admin role
        if (context.User.IsInRole(Roles.SystemAdministrator))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has the required permission
        var hasPermission = context.User.HasPermission(requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}