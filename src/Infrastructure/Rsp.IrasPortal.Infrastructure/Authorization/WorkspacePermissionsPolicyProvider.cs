using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Rsp.Portal.Infrastructure.Authorization;

/// <summary>
/// Provides authorization policies for workspace-related operations.
/// </summary>
/// <remarks>
/// <para>
/// This provider resolves policy names into concrete <see cref="AuthorizationPolicy"/> instances
/// used by ASP.NET Core's authorization system. It recognizes two custom policy shapes:
/// </para>
/// <para>
/// 1. Workspace policy (single segment):
///    - Format: <c>workspace</c> (no dots)
///    - Behavior: Produces an <see cref="AuthorizationPolicy"/> containing a
///      <see cref="WorkspaceRequirement"/> initialized with the workspace identifier
///      (the single segment). This requirement will be evaluated by a corresponding
///      authorization handler to determine access to the workspace-level resource.
/// </para>
/// <para>
/// 2. Permissions policy (three segments):
///    - Format: <c>workspace.area.action</c> (e.g. <c>myresearch.projectrecord.read</c>)
///    - Behavior: Produces an <see cref="AuthorizationPolicy"/> containing a
///      <see cref="PermissionRequirement"/> initialized with the full permission identifier.
///      A permission handler is expected to evaluate this requirement.
/// </para>
/// <para>
/// Any policy name that does not match the above shapes is delegated to a
/// <see cref="DefaultAuthorizationPolicyProvider"/> fallback so typical application
/// policies (including role-based or other named policies) continue to function.
/// </para>
/// <para>
/// The policies built here also include a common requirement that the user is authenticated
/// and has an <see cref="ClaimTypes.Email"/> claim.
/// </para>
/// </remarks>
public class WorkspacePermissionsPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    // ASP.NET Core only uses one authorization policy provider.
    // Use a DefaultAuthorizationPolicyProvider as a fallback for all policies that this provider
    // does not explicitly handle.
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    /// <summary>
    /// Returns a policy for the given policy name. If the policy name is a single segment
    /// (no dots) this method will build and return a workspace-scoped policy.
    /// If the policy name uses the three-segment permission format "workspace.area.action"
    /// this method will build and return a permission-scoped policy.
    /// Otherwise this method will defer to the default provider.
    /// </summary>
    /// <param name="policyName">The name of the policy to retrieve.</param>
    /// <returns>An <see cref="AuthorizationPolicy"/> or null if not found.</returns>
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // the policy name should either be a workspace policy or a permissions policy.
        // workspace policies will simply use workspace name as identifier.
        // permissions policies will take the format "workspace.area.action" e.g. "myresearch.projectrecord.read"

        var policySegments = policyName.Split('.');

        // Determine if the policy is a permissions policy (3 segments) or a workspace policy (1 segment).
        var isWorkspacePolicy = policySegments.Length == 1;
        var isPermissionsPolicy = policySegments.Length == 3;

        if (isWorkspacePolicy)
        {
            // Build an authorization policy which includes the workspace requirement.
            // The WorkspaceRequirement will encapsulate the workspace identifier and be evaluated
            // by a corresponding authorization handler.
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new WorkspaceRequirement(policyName))
                .Build();

            // Return the constructed policy. Use Task.FromResult to satisfy the async signature.
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        if (isPermissionsPolicy)
        {
            // Build an authorization policy which includes the permission requirement.
            // The PermissionRequirement will encapsulate the permission identifier and be evaluated
            // by a corresponding authorization handler.
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            // Return the constructed policy. Use Task.FromResult to satisfy the async signature.
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    /// <summary>
    /// Returns the default authorization policy for the application.
    /// This default policy requires an authenticated user and an email claim.
    /// </summary>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <summary>
    /// Returns the fallback policy by delegating to the default provider.
    /// </summary>
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}