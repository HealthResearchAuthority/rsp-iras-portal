using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Rsp.IrasPortal.Application.AccessControl;

namespace Rsp.IrasPortal.Infrastructure.Authorization;

/// <summary>
/// Handler that checks if a user can access a record based on its status and their roles
/// </summary>
public class RecordStatusAuthorizationHandler : AuthorizationHandler<RecordStatusRequirement>
{
    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context,
        RecordStatusRequirement requirement
    )
    {
        // Get all roles from the user's claims
        var userRoles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!userRoles.Any())
        {
            return Task.CompletedTask;
        }

        // Get allowed statuses based on entity type
        List<string> allowedStatuses = requirement.EntityType.ToLowerInvariant() switch
        {
            "projectrecord" => RoleStatusPermissions.ProjectRecord.GetAllowedStatuses(userRoles),
            "modification" => RoleStatusPermissions.Modification.GetAllowedStatuses(userRoles),
            "document" => RoleStatusPermissions.Document.GetAllowedStatuses(userRoles),
            _ => new List<string>()
        };

        // Check if the record's status is in the allowed list
        if (allowedStatuses.Contains(requirement.Status, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}