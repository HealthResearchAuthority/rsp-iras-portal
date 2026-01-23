using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Rsp.Portal.Application.AccessControl;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Infrastructure.Authorization;

namespace Rsp.Portal.UnitTests.Infrastructure.Authorization.PermissionRequirementHandlerTests;

public class PermissionRequirementHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_Succeeds_For_SystemAdministrator_For_Any_Permission()
    {
        // Arrange
        var handler = new PermissionRequirementHandler();
        var user = CreateUser(Roles.SystemAdministrator);
        var requirement = new PermissionRequirement("any.permission");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_Permission_Claim_Matching()
    {
        // Arrange
        var handler = new PermissionRequirementHandler();
        var permission = Permissions.MyResearch.ProjectRecord_Read;
        var user = CreateUser(Roles.Applicant);
        var requirement = new PermissionRequirement(permission);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Has_No_Permissions()
    {
        // Arrange
        var handler = new PermissionRequirementHandler();
        var user = CreateUser();
        var requirement = new PermissionRequirement(Permissions.MyResearch.ProjectRecord_Read);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_Permission_Does_Not_Match()
    {
        // Arrange
        var handler = new PermissionRequirementHandler();
        var user = CreateUser(Roles.Sponsor);
        var requirement = new PermissionRequirement(Permissions.MyResearch.ProjectRecord_Create); // Sponsor does not have create
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_Multiple_Permissions_One_Matches()
    {
        // Arrange
        var handler = new PermissionRequirementHandler();
        var user = CreateUser(Roles.Sponsor, Roles.Applicant);
        var requirement = new PermissionRequirement(Permissions.MyResearch.ProjectRecord_Create); // Applicant provides create
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Is_Case_Sensitive_For_Permission_Names()
    {
        // Arrange
        var handler = new PermissionRequirementHandler();
        var user = CreateUser(Roles.Applicant);
        var requirement = new PermissionRequirement(Permissions.MyResearch.ProjectRecord_Read.ToUpperInvariant()); // different case
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_For_Unauthenticated_Principal_If_Permission_Claim_Present()
    {
        // Arrange
        var handler = new PermissionRequirementHandler();
        // Create an unauthenticated identity (authentication type == null)
        var claims = new[] { new Claim(CustomClaimTypes.Permissions, "some.permission") };
        var identity = new ClaimsIdentity(claims); // IsAuthenticated == false
        var user = new ClaimsPrincipal(identity);

        var requirement = new PermissionRequirement("some.permission");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        // Current handler implementation does not check IsAuthenticated, it only checks claims
        context.HasSucceeded.ShouldBeTrue();
    }

    private static ClaimsPrincipal CreateUser(params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

        // Add permission claims derived from roles so HasPermission (which checks "permissions" claims)
        // will work with role-based tests
        if (roles.Length > 0)
        {
            var perms = RolePermissions.GetPermissionsForRoles(roles);
            claims.AddRange(perms.Select(p => new Claim(CustomClaimTypes.Permissions, p)));

            // Add allowed status claims for each entity type
            var allowedStatuses = RoleStatusPermissions.GetAllowedStatusesForRoles(roles);
            foreach (var (entityType, statuses) in allowedStatuses)
            {
                claims.AddRange(statuses.Select(status => new Claim($"allowed_statuses/{entityType}", status)));
            }
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}