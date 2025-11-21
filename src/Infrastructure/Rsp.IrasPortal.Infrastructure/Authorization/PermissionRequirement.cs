using Microsoft.AspNetCore.Authorization;

namespace Rsp.IrasPortal.Infrastructure.Authorization;

/// <summary>
/// Requirement that validates if a user has a specific permission string (workspace.area.action)
/// </summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}