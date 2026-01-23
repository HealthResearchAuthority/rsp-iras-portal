using Microsoft.AspNetCore.Authorization;

namespace Rsp.Portal.Infrastructure.Authorization;

/// <summary>
/// Requirement that validates if a user has a specific permission
/// </summary>
public class WorkspaceRequirement(string workspace) : IAuthorizationRequirement
{
    public string Workspace { get; } = workspace;
}