using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Infrastructure.Authorization;

namespace Rsp.IrasPortal.UnitTests.Infrastructure.Authorization.WorkspaceRequirementHandlerTests;

public class WorkspaceRequirementHandlerTests
{
    // HandleRequirementAsync Tests - SystemAdministrator Has Universal Access

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_For_SystemAdministrator_For_Any_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.SystemAdministrator);
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_For_SystemAdministrator_Even_For_Unknown_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.SystemAdministrator);
        var requirement = new WorkspaceRequirement("unknownworkspace");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_For_SystemAdministrator_With_Empty_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.SystemAdministrator);
        var requirement = new WorkspaceRequirement("");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    // HandleRequirementAsync Tests - MyResearch Workspace

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_Applicant_Role_For_MyResearch_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant);
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_Sponsor_Role_For_MyResearch_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Sponsor);
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_WorkflowCoordinator_Role_For_MyResearch_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.WorkflowCoordinator);
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_TeamManager_Role_For_MyResearch_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.TeamManager);
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_StudyWideReviewer_Role_For_MyResearch_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.StudyWideReviewer);
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    // HandleRequirementAsync Tests - Sponsor Workspace

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_Sponsor_Role_For_Sponsor_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Sponsor);
        var requirement = new WorkspaceRequirement(Workspaces.Sponsor);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Has_Applicant_Role_For_Sponsor_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant);
        var requirement = new WorkspaceRequirement(Workspaces.Sponsor);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    // HandleRequirementAsync Tests - SystemAdministration Workspace

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Has_Applicant_Role_For_SystemAdministration_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant);
        var requirement = new WorkspaceRequirement(Workspaces.SystemAdministration);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Has_Sponsor_Role_For_SystemAdministration_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Sponsor);
        var requirement = new WorkspaceRequirement(Workspaces.SystemAdministration);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    // HandleRequirementAsync Tests - Approvals Workspace

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_TeamManager_Role_For_Approvals_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.TeamManager);
        var requirement = new WorkspaceRequirement(Workspaces.Approvals);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_StudyWideReviewer_Role_For_Approvals_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.StudyWideReviewer);
        var requirement = new WorkspaceRequirement(Workspaces.Approvals);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_WorkflowCoordinator_Role_For_Approvals_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.WorkflowCoordinator);
        var requirement = new WorkspaceRequirement(Workspaces.Approvals);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Has_Applicant_Role_For_Approvals_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant);
        var requirement = new WorkspaceRequirement(Workspaces.Approvals);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    // HandleRequirementAsync Tests - Multiple Roles

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_Multiple_Roles_With_One_Allowed_For_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant, Roles.StudyWideReviewer);
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Has_Multiple_Roles_But_None_Allowed_For_Workspace()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant, Roles.StudyWideReviewer);
        var requirement = new WorkspaceRequirement(Workspaces.SystemAdministration);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_When_User_Has_SystemAdministrator_Plus_Other_Roles()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant, Roles.SystemAdministrator);
        var requirement = new WorkspaceRequirement("anyworkspace");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    // HandleRequirementAsync Tests - Unknown Workspace

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_Workspace_Is_Not_In_Matrix()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant);
        var requirement = new WorkspaceRequirement("unknownworkspace");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_Workspace_Is_Empty_String()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant);
        var requirement = new WorkspaceRequirement("");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    // HandleRequirementAsync Tests - No Roles

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Has_No_Roles()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser();
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_When_User_Principal_Is_Not_Authenticated()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    // HandleRequirementAsync Tests - Case Sensitivity

    [Fact]
    public async Task HandleRequirementAsync_Is_Case_Sensitive_For_Role_Names()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser("Applicant"); // Capital A
        var requirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    // Integration Tests - Real World Scenarios
    [Fact]
    public async Task Integration_Sponsor_Can_Access_Both_MyResearch_And_Sponsor_Workspaces()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Sponsor);

        var myResearchRequirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var myResearchContext = new AuthorizationHandlerContext([myResearchRequirement], user, null);

        var sponsorRequirement = new WorkspaceRequirement(Workspaces.Sponsor);
        var sponsorContext = new AuthorizationHandlerContext([sponsorRequirement], user, null);

        // Act
        await handler.HandleAsync(myResearchContext);
        await handler.HandleAsync(sponsorContext);

        // Assert
        myResearchContext.HasSucceeded.ShouldBeTrue();
        sponsorContext.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Integration_SystemAdministrator_Can_Access_All_Workspaces_Including_Undefined_Ones()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.SystemAdministrator);

        var workspaces = new[]
        {
            Workspaces.MyResearch,
            Workspaces.Sponsor,
            Workspaces.SystemAdministration,
            Workspaces.Approvals,
            "undefined-workspace" // SystemAdministrator should have access even to undefined workspaces
        };

        // Act & Assert
        foreach (var workspace in workspaces)
        {
            var requirement = new WorkspaceRequirement(workspace);
            var context = new AuthorizationHandlerContext([requirement], user, null);
            await handler.HandleAsync(context);
            context.HasSucceeded.ShouldBeTrue($"SystemAdministrator should have access to {workspace}");
        }
    }

    [Fact]
    public async Task Integration_TeamManager_Can_Access_MyResearch_And_Approvals_Workspaces()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.TeamManager);

        var myResearchRequirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var myResearchContext = new AuthorizationHandlerContext([myResearchRequirement], user, null);

        var approvalsRequirement = new WorkspaceRequirement(Workspaces.Approvals);
        var approvalsContext = new AuthorizationHandlerContext([approvalsRequirement], user, null);

        var sponsorRequirement = new WorkspaceRequirement(Workspaces.Sponsor);
        var sponsorContext = new AuthorizationHandlerContext([sponsorRequirement], user, null);

        // Act
        await handler.HandleAsync(myResearchContext);
        await handler.HandleAsync(approvalsContext);
        await handler.HandleAsync(sponsorContext);

        // Assert
        myResearchContext.HasSucceeded.ShouldBeTrue();
        approvalsContext.HasSucceeded.ShouldBeTrue();
        sponsorContext.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Integration_User_With_Multiple_Roles_Has_Combined_Workspace_Access()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.Applicant, Roles.TeamManager);

        var myResearchRequirement = new WorkspaceRequirement(Workspaces.MyResearch);
        var myResearchContext = new AuthorizationHandlerContext([myResearchRequirement], user, null);

        var approvalsRequirement = new WorkspaceRequirement(Workspaces.Approvals);
        var approvalsContext = new AuthorizationHandlerContext([approvalsRequirement], user, null);

        // Act
        await handler.HandleAsync(myResearchContext);
        await handler.HandleAsync(approvalsContext);

        // Assert
        myResearchContext.HasSucceeded.ShouldBeTrue(); // From Applicant role
        approvalsContext.HasSucceeded.ShouldBeTrue(); // From TeamManager role
    }

    [Fact]
    public async Task Integration_SystemAdministrator_Bypasses_Workspace_Matrix_Check()
    {
        // Arrange
        var handler = new WorkspaceRequirementHandler();
        var user = CreateUser(Roles.SystemAdministrator);

        // Test with a workspace that doesn't exist in the matrix
        var requirement = new WorkspaceRequirement("completely-unknown-workspace-12345");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue("SystemAdministrator should bypass workspace matrix lookup");
    }

    // Helper Methods

    private static ClaimsPrincipal CreateUser(params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}