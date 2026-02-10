using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Rsp.IrasPortal.Application.FeatureFilters;

/// <summary>
/// Provides targeting context for feature flags based on the current HTTP request.
/// Implements user and group-based feature targeting using claims and request headers.
/// </summary>
/// <param name="httpContextAccessor">Accessor for retrieving the current HTTP context.</param>
[ExcludeFromCodeCoverage]
public sealed class UserTargetingContext(IHttpContextAccessor httpContextAccessor) : ITargetingContextAccessor
{
    /// <summary>
    /// Retrieves the targeting context for the current user asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TargetingContext}"/> containing the user's targeting information.</returns>
    public ValueTask<TargetingContext> GetContextAsync()
    {
        // Get the current HTTP context
        var httpContext = httpContextAccessor.HttpContext!;

        // Build the targeting context with user ID and groups
        var targetingContext = new TargetingContext
        {
            UserId = GetUserId(httpContext),
            Groups = GetUserGroups(httpContext)
        };

        return new ValueTask<TargetingContext>(targetingContext);
    }

    /// <summary>
    /// Extracts the user ID from the HTTP context using the email claim.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The user's email address if available; otherwise, an empty string.</returns>
    private static string GetUserId(HttpContext? httpContext)
    {
        // Retrieve the email claim from the authenticated user
        var emailClaim = httpContext?.User.FindFirst(ClaimTypes.Email);

        return emailClaim?.Value ?? string.Empty;
    }

    /// <summary>
    /// Retrieves the user's group from the request headers.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>An array of group names the user belongs to.</returns>
    private static string[] GetUserGroups(HttpContext? httpContext)
    {
        // Extract comma-separated groups from the x-feature-groups header
        var userGroups = httpContext?.Request
                             .Headers["x-feature-groups"]
                             .FirstOrDefault()?
                             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         ?? [];

        return userGroups;
    }
}