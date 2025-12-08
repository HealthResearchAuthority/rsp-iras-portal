using System.Security.Claims;
using Rsp.IrasPortal.Application.AccessControl;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Extensions;
using Rsp.IrasPortal.Domain.AccessControl;

namespace Rsp.IrasPortal.UnitTests.Application.Extensions.ClaimsPrincipalExtensionsTests;

public class ClaimsPrincipalExtensionsTests
{
    // HasPermission Tests

    [Fact]
    public void HasPermission_Returns_True_When_User_Has_Permission_Through_Single_Role()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var result = user.HasPermission(Permissions.MyResearch.Workspace_Access);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasPermission_Returns_False_When_User_Does_Not_Have_Permission()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var result = user.HasPermission(Permissions.SystemAdministration.Workspace_Access);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasPermission_Returns_True_When_User_Has_Multiple_Roles_With_Permission()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant, Roles.Sponsor);

        // Act
        var result = user.HasPermission(Permissions.MyResearch.ProjectRecord_Read);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasPermission_Returns_False_When_User_Has_No_Roles()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = user.HasPermission(Permissions.MyResearch.Workspace_Access);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasPermission_Returns_True_For_System_Administrator_With_All_Workspace_Permissions()
    {
        // Arrange
        var user = CreateUser(Roles.SystemAdministrator);

        // Act & Assert
        user.HasPermission(Permissions.MyResearch.Workspace_Access).ShouldBeTrue();
        user.HasPermission(Permissions.Sponsor.Workspace_Access).ShouldBeTrue();
        user.HasPermission(Permissions.Approvals.Workspace_Access).ShouldBeTrue();
        user.HasPermission(Permissions.SystemAdministration.Workspace_Access).ShouldBeTrue();
    }

    [Fact]
    public void HasPermission_Handles_Case_Sensitive_Role_Names()
    {
        // Arrange
        var user = CreateUser("Applicant"); // Note: capital A

        // Act
        var result = user.HasPermission(Permissions.MyResearch.Workspace_Access);

        // Assert
        result.ShouldBeFalse(); // Roles are case-sensitive
    }

    // CanAccessRecordStatus Tests

    [Fact]
    public void CanAccessRecordStatus_Returns_True_When_User_Can_Access_Status_For_ProjectRecord()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var result = user.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.InDraft);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessRecordStatus_Returns_False_When_User_Cannot_Access_Status()
    {
        // Arrange
        var user = CreateUser(Roles.Sponsor);

        // Act
        var result = user.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.InDraft);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAccessRecordStatus_Is_Case_Sensitive_For_Status()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var result = user.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, "IN DRAFT");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAccessRecordStatus_Works_For_Modification_Entity_Type()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var result = user.CanAccessRecordStatus(StatusEntitiy.Modification, ModificationStatus.WithSponsor);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessRecordStatus_Works_For_Document_Entity_Type()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var result = user.CanAccessRecordStatus(StatusEntitiy.Document, DocumentStatus.Uploaded);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessRecordStatus_Returns_False_For_Unknown_Entity_Type()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var result = user.CanAccessRecordStatus("unknowntype", "some status");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAccessRecordStatus_Combines_Statuses_From_Multiple_Roles()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant, Roles.Sponsor);

        // Act
        var canAccessDraft = user.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.InDraft);
        var canAccessActive = user.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.Active);

        // Assert
        canAccessDraft.ShouldBeTrue(); // Applicant can access
        canAccessActive.ShouldBeTrue(); // Both can access
    }

    // GetUserPermissions Tests

    [Fact]
    public void GetUserPermissions_Returns_All_Permissions_For_Single_Role()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var permissions = user.GetUserPermissions();

        // Assert
        permissions.ShouldNotBeEmpty();
        permissions.ShouldContain(Permissions.MyResearch.Workspace_Access);
        permissions.ShouldContain(Permissions.MyResearch.ProjectRecord_Create);
        permissions.ShouldContain(Permissions.MyResearch.Modifications_Create);
    }

    [Fact]
    public void GetUserPermissions_Returns_Empty_List_When_User_Has_No_Roles()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var permissions = user.GetUserPermissions();

        // Assert
        permissions.ShouldBeEmpty();
    }

    [Fact]
    public void GetUserPermissions_Returns_Combined_Permissions_For_Multiple_Roles()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant, Roles.Sponsor);

        // Act
        var permissions = user.GetUserPermissions();

        // Assert
        permissions.ShouldNotBeEmpty();
        // Should have permissions from both roles
        permissions.ShouldContain(Permissions.MyResearch.ProjectRecord_Create); // Applicant
        permissions.ShouldContain(Permissions.Sponsor.Workspace_Access); // Sponsor
    }

    [Fact]
    public void GetUserPermissions_Returns_Distinct_Permissions_When_Roles_Share_Permissions()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant, Roles.Sponsor);

        // Act
        var permissions = user.GetUserPermissions();

        // Assert
        // Both roles have MyResearch.ProjectRecord_Read, should only appear once
        var readPermCount = permissions.Count(p => p == Permissions.MyResearch.ProjectRecord_Read);
        readPermCount.ShouldBe(1);
    }

    [Fact]
    public void GetUserPermissions_Returns_No_Explicit_Permissions_For_System_Administrator()
    {
        // Arrange
        var user = CreateUser(Roles.SystemAdministrator);

        // Act
        var permissions = user.GetUserPermissions();

        // Assert
        permissions.ShouldBeEmpty();
    }

    // GetAllowedStatuses Tests

    [Fact]
    public void GetAllowedStatuses_Returns_Statuses_For_ProjectRecord_Entity()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var statuses = user.GetAllowedStatuses(StatusEntitiy.ProjectRecord);

        // Assert
        statuses.ShouldNotBeEmpty();
        statuses.ShouldContain(ProjectRecordStatus.InDraft);
        statuses.ShouldContain(ProjectRecordStatus.Active);
    }

    [Fact]
    public void GetAllowedStatuses_Returns_Statuses_For_Modification_Entity()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var statuses = user.GetAllowedStatuses(StatusEntitiy.Modification);

        // Assert
        statuses.ShouldNotBeEmpty();
        statuses.ShouldContain(ModificationStatus.InDraft);
        statuses.ShouldContain(ModificationStatus.WithSponsor);
        statuses.ShouldContain(ModificationStatus.WithReviewBody);
    }

    [Fact]
    public void GetAllowedStatuses_Returns_Statuses_For_Document_Entity()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var statuses = user.GetAllowedStatuses(StatusEntitiy.Document);

        // Assert
        statuses.ShouldNotBeEmpty();
        statuses.ShouldContain(DocumentStatus.Uploaded);
        statuses.ShouldContain(DocumentStatus.Complete);
    }

    [Fact]
    public void GetAllowedStatuses_Returns_Empty_List_For_Unknown_Entity_Type()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var statuses = user.GetAllowedStatuses("unknowntype");

        // Assert
        statuses.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllowedStatuses_Is_Case_Insensitive_For_Entity_Type()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant);

        // Act
        var statusesLower = user.GetAllowedStatuses("projectrecord");
        var statusesUpper = user.GetAllowedStatuses("PROJECTRECORD");
        var statusesMixed = user.GetAllowedStatuses("ProjectRecord");

        // Assert
        statusesLower.ShouldNotBeEmpty();
        statusesUpper.ShouldNotBeEmpty();
        statusesMixed.ShouldNotBeEmpty();
        statusesLower.Count.ShouldBe(statusesUpper.Count);
        statusesLower.Count.ShouldBe(statusesMixed.Count);
    }

    [Fact]
    public void GetAllowedStatuses_Combines_Statuses_From_Multiple_Roles()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant, Roles.Sponsor);

        // Act
        var statuses = user.GetAllowedStatuses(StatusEntitiy.ProjectRecord);

        // Assert
        statuses.ShouldNotBeEmpty();
        // Should have statuses from both roles
        statuses.ShouldContain(ProjectRecordStatus.InDraft); // Applicant only
        statuses.ShouldContain(ProjectRecordStatus.Active); // Both
    }

    [Fact]
    public void GetAllowedStatuses_Returns_Empty_List_When_User_Has_No_Roles()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var statuses = user.GetAllowedStatuses(StatusEntitiy.ProjectRecord);

        // Assert
        statuses.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllowedStatuses_Returns_Distinct_Statuses_When_Roles_Share_Statuses()
    {
        // Arrange
        var user = CreateUser(Roles.WorkflowCoordinator, Roles.TeamManager);

        // Act
        var statuses = user.GetAllowedStatuses(StatusEntitiy.Modification);

        // Assert
        statuses.ShouldNotBeEmpty();
        // "With review body" should only appear once
        var withReviewBodyCount = statuses.Count(s => s == ModificationStatus.WithReviewBody);
        withReviewBodyCount.ShouldBe(1);
    }

    // Integration Tests - Real World Scenarios

    [Fact]
    public void Applicant_Can_Access_Their_Own_Draft_Projects()
    {
        // Arrange
        var applicant = CreateUser(Roles.Applicant);

        // Act & Assert
        applicant.HasPermission(Permissions.MyResearch.ProjectRecord_Create).ShouldBeTrue();
        applicant.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.InDraft).ShouldBeTrue();
    }

    [Fact]
    public void Sponsor_Cannot_Create_Projects_But_Can_View_Active_Ones()
    {
        // Arrange
        var sponsor = CreateUser(Roles.Sponsor);

        // Act & Assert
        sponsor.HasPermission(Permissions.MyResearch.ProjectRecord_Create).ShouldBeFalse();
        sponsor.HasPermission(Permissions.MyResearch.ProjectRecord_Read).ShouldBeTrue();
        sponsor.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.InDraft).ShouldBeFalse();
        sponsor.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.Active).ShouldBeTrue();
    }

    [Fact]
    public void Team_Manager_Can_Access_Review_Workflow_Modifications()
    {
        // Arrange
        var teamManager = CreateUser(Roles.TeamManager);

        // Act & Assert
        teamManager.HasPermission(Permissions.Approvals.Workspace_Access).ShouldBeTrue();
        teamManager.CanAccessRecordStatus(StatusEntitiy.Modification, ModificationStatus.WithReviewBody).ShouldBeTrue();
        teamManager.CanAccessRecordStatus(StatusEntitiy.Modification, ModificationStatus.Approved).ShouldBeTrue();
    }

    [Fact]
    public void User_With_Multiple_Roles_Has_Combined_Permissions()
    {
        // Arrange
        var user = CreateUser(Roles.Applicant, Roles.TeamManager);

        // Act & Assert
        // Should have applicant permissions
        user.HasPermission(Permissions.MyResearch.ProjectRecord_Create).ShouldBeTrue();
        user.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.InDraft).ShouldBeTrue();

        // Should also have team manager permissions
        user.HasPermission(Permissions.Approvals.Workspace_Access).ShouldBeTrue();
        user.HasPermission(Permissions.Approvals.Modifications_Assign).ShouldBeTrue();

        // Should have combined statuses
        var modStatuses = user.GetAllowedStatuses(StatusEntitiy.Modification);
        modStatuses.ShouldContain(ModificationStatus.InDraft); // From applicant
        modStatuses.ShouldContain(ModificationStatus.WithReviewBody); // From team manager
    }

    // Edge Cases and Null Handling

    [Fact]
    public void Extensions_Work_With_Unauthenticated_User()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated

        // Act & Assert
        user.HasPermission(Permissions.MyResearch.Workspace_Access).ShouldBeFalse();
        user.GetUserPermissions().ShouldBeEmpty();
        user.GetAllowedStatuses(StatusEntitiy.ProjectRecord).ShouldBeEmpty();
        user.CanAccessRecordStatus(StatusEntitiy.ProjectRecord, ProjectRecordStatus.Active).ShouldBeFalse();
    }

    // Helper Methods

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