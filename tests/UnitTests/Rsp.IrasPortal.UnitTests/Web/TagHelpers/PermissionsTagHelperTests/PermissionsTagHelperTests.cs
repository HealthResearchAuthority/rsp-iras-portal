using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.Portal.Application.AccessControl;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.TagHelpers;

namespace Rsp.Portal.UnitTests.Web.TagHelpers.PermissionsTagHelperTests;

public class PermissionsTagHelperTests
{
    // Validation Tests

    [Fact]
    public void HasPermissionAttributes_Throws_When_Both_Permission_And_Permissions_Are_Set()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permission = "perm",
            Permissions = ["a"]
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => tagHelper.HasPermissionAttributes());
    }

    [Fact]
    public void HasPermissionAttributes_Returns_True_When_Only_Permission_Is_Set()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permission = "perm"
        };

        // Act & Assert
        tagHelper.HasPermissionAttributes().ShouldBeTrue();
    }

    [Fact]
    public void HasPermissionAttributes_Returns_True_When_Only_Permissions_Are_Set()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions = ["a", "b"]
        };

        // Act & Assert
        tagHelper.HasPermissionAttributes().ShouldBeTrue();
    }

    [Fact]
    public void HasRoleAttributes_Throws_When_Both_Role_And_Roles_Are_Set()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Role = "r",
            RolesList = ["x"]
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => tagHelper.HasRoleAttributes());
    }

    [Fact]
    public void HasRoleAttributes_Returns_True_When_Only_Role_Is_Set()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Role = "r"
        };

        // Act & Assert
        tagHelper.HasRoleAttributes().ShouldBeTrue();
    }

    [Fact]
    public void HasRoleAttributes_Returns_True_When_Only_Roles_Are_Set()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            RolesList = ["r1", "r2"]
        };

        // Act & Assert
        tagHelper.HasRoleAttributes().ShouldBeTrue();
    }

    [Fact]
    public void EvaluatePermissionsAndRoles_Throws_When_Both_Permissions_And_Roles_Are_Specified()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permission = "p",
            RolesList = ["r"]
        };

        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth")) };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => tagHelper.EvaluatePermissionsAndRoles());
    }

    [Fact]
    public void EvaluatePermissionsAndRoles_Returns_True_When_No_Permissions_Or_Roles_Are_Specified()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper();

        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth")) };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act & Assert
        tagHelper.EvaluatePermissionsAndRoles().ShouldBeTrue();
    }

    // Permission Evaluation Tests

    [Fact]
    public void EvaluatePermissions_Returns_True_For_Single_Permission_When_User_Has_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permission = Permissions.MyResearch.Workspace_Access
        };

        // User with applicant role which has MyResearch.Workspace_Access permission
        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluatePermissions().ShouldBeTrue();
    }

    [Fact]
    public void EvaluatePermissions_Returns_False_For_Single_Permission_When_User_Does_Not_Have_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permission = Permissions.SystemAdministration.Workspace_Access
        };

        // User with applicant role which does NOT have SystemAdministration.Workspace_Access
        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluatePermissions().ShouldBeFalse();
    }

    [Fact]
    public void EvaluatePermissions_Any_Returns_True_When_User_Has_One_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.MyResearch.Workspace_Access,
                Permissions.SystemAdministration.Workspace_Access
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.Any
        };

        // User with applicant role - has MyResearch but not SystemAdministration
        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluatePermissions().ShouldBeTrue();
    }

    [Fact]
    public void EvaluatePermissions_Any_Returns_False_When_User_Has_No_Permissions()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.SystemAdministration.Workspace_Access,
                Permissions.Approvals.Workspace_Access
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.Any
        };

        // User with applicant role - has neither SystemAdministration nor Approvals access
        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluatePermissions().ShouldBeFalse();
    }

    [Fact]
    public void EvaluatePermissions_All_Returns_True_When_User_Has_All_Permissions()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.MyResearch.ProjectRecord_Read,
                Permissions.MyResearch.ProjectRecord_Create
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All
        };

        // User with applicant role - has both permissions
        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluatePermissions().ShouldBeTrue();
    }

    [Fact]
    public void EvaluatePermissions_All_Returns_False_When_User_Missing_One_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.MyResearch.ProjectRecord_Read,
                Permissions.SystemAdministration.Workspace_Access
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All
        };

        // User with applicant role - has first permission but not second
        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluatePermissions().ShouldBeFalse();
    }

    [Fact]
    public void EvaluatePermissions_Works_With_System_Administrator_Having_All_Workspace_Permissions()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.MyResearch.Workspace_Access,
                Permissions.Sponsor.Workspace_Access,
                Permissions.Approvals.Workspace_Access,
                Permissions.SystemAdministration.Workspace_Access
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All
        };

        // User with system_administrator role - has all workspace permissions
        var principal = CreateUser(Roles.SystemAdministrator);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluatePermissions().ShouldBeTrue();
    }

    // Role Evaluation Tests

    [Fact]
    public void EvaluateRoles_Returns_True_For_Single_Role_When_User_Is_In_Role()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Role = Roles.TeamManager
        };

        var principal = CreateUser(Roles.TeamManager);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluateRoles().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateRoles_Returns_False_For_Single_Role_When_User_Not_In_Role()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Role = "admin"
        };

        var principal = CreateUser("user");
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluateRoles().ShouldBeFalse();
    }

    [Fact]
    public void EvaluateRoles_Any_Returns_True_When_User_Has_One_Role()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            RolesList = ["a", "b"],
            RoleEvaluationLogic = PermissionsTagHelper.RoleMode.Any
        };

        var principal = CreateUser("b");
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluateRoles().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateRoles_All_Returns_True_When_User_Has_All_Roles()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            RolesList = ["x", "y"],
            RoleEvaluationLogic = PermissionsTagHelper.RoleMode.All
        };

        var principal = CreateUser("x", "y");
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluateRoles().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateRoles_All_Returns_False_When_User_Missing_One_Role()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            RolesList = ["x", "y"],
            RoleEvaluationLogic = PermissionsTagHelper.RoleMode.All
        };

        var principal = CreateUser("x");
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        tagHelper.EvaluateRoles().ShouldBeFalse();
    }

    // Status Access Tests

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_StatusFor_Is_Null()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper();

        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth")) };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act & Assert
        tagHelper.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_StatusEntity_Is_Empty()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            StatusEntity = ""
        };

        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth")) };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act & Assert
        tagHelper.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_User_Can_Access_ProjectRecord_Status()
    {
        // Arrange
        var statusValue = ProjectRecordStatus.InDraft;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.ProjectRecord
        };

        var principal = CreateUser(Roles.Applicant);
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_False_When_User_Cannot_Access_ProjectRecord_Status()
    {
        // Arrange
        var statusValue = ProjectRecordStatus.InDraft;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.ProjectRecord
        };

        var principal = CreateUser(Roles.Sponsor); // Sponsor cannot access "In draft"
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_User_Can_Access_Modification_Status()
    {
        // Arrange
        var statusValue = ModificationStatus.WithSponsor;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.Modification
        };

        var principal = CreateUser(Roles.Sponsor);
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_False_When_User_Cannot_Access_Modification_Status()
    {
        // Arrange
        var statusValue = ModificationStatus.WithSponsor;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.Modification
        };

        var principal = CreateUser(Roles.TeamManager); // TeamManager cannot access "With sponsor"
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_User_Can_Access_Document_Status()
    {
        // Arrange
        var statusValue = DocumentStatus.Uploaded;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.Document
        };

        var principal = CreateUser(Roles.Applicant);
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Is_Case_Sensitive_For_Status()
    {
        // Arrange
        var statusValue = "IN DRAFT";
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.ProjectRecord
        };

        var principal = CreateUser(Roles.Applicant);
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_False_For_Unknown_Entity_Type()
    {
        // Arrange
        var statusValue = "Some Status";
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = "unknownentity"
        };

        var principal = CreateUser(Roles.Applicant);
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_SystemAdministrator_Can_Access_Any_Status()
    {
        // Arrange
        var statusValue = ModificationStatus.WithReviewBody;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.Modification
        };

        var principal = CreateUser(Roles.SystemAdministrator);
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Combines_Statuses_From_Multiple_Roles()
    {
        // Arrange
        var statusValue = ModificationStatus.InDraft;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.Modification
        };

        // User with both applicant and sponsor roles
        var principal = CreateUser(Roles.Applicant, Roles.Sponsor);
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        // Act
        var result = tagHelper.UserCanAccessStatus();

        // Assert
        result.ShouldBeTrue(); // Applicant can access "In draft"
    }

    // Process Tests - Authentication

    [Fact]
    public void Process_Shows_Content_When_ShowWhenUserIsNotAuthenticated_And_User_Is_Anonymous()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            ShowWhenUserIsNotAuthenticated = true
        };

        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "authorized",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("child content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.TagName.ShouldBeNull(); // standalone tag wrapper removed
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public void Process_Suppresses_Content_When_ShowWhenUserIsNotAuthenticated_And_User_Is_Authenticated()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            ShowWhenUserIsNotAuthenticated = true
        };

        var principal = CreateUser(Roles.TeamManager);

        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "authorized-when",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("child content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public void Process_Suppresses_Content_When_User_Is_Not_Authenticated_And_No_ShowFlag()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper();

        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("test content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    // Process Tests - Role-Based

    [Fact]
    public void Process_Shows_Content_When_User_Is_Authenticated_And_Role_Matches()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Role = Roles.TeamManager
        };

        var principal = CreateUser(Roles.TeamManager);

        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("visible content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public void Process_Suppresses_Content_When_User_Does_Not_Have_Required_Role()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Role = "admin"
        };

        var principal = CreateUser("user");

        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("admin content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public void Process_Shows_Content_When_RolesList_Any_Mode_And_User_Has_One_Role()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            RolesList = ["a", "b"],
            RoleEvaluationLogic = PermissionsTagHelper.RoleMode.Any
        };

        var principal = CreateUser("b");
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("any content");

        // Act
        tagHelper.Process(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public void Process_Suppresses_Content_When_RolesList_All_Mode_And_User_Missing_One_Role()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            RolesList = ["x", "y"],
            RoleEvaluationLogic = PermissionsTagHelper.RoleMode.All
        };

        var principal = CreateUser("x");
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("all content");

        // Act
        tagHelper.Process(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    // Process Tests - Permission-Based

    [Fact]
    public void Process_Shows_Content_When_User_Has_Required_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permission = Permissions.MyResearch.Workspace_Access
        };

        var principal = CreateUser(Roles.Applicant);

        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("workspace content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public void Process_Suppresses_Content_When_User_Does_Not_Have_Required_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permission = Permissions.SystemAdministration.Workspace_Access
        };

        var principal = CreateUser(Roles.Applicant);

        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("admin workspace content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public void Process_Shows_Content_When_PermissionsList_Any_Mode_And_User_Has_One_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.MyResearch.Workspace_Access,
                Permissions.SystemAdministration.Workspace_Access
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.Any
        };

        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("any permission content");

        // Act
        tagHelper.Process(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public void Process_Suppresses_Content_When_PermissionsList_All_Mode_And_User_Missing_One_Permission()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.MyResearch.Workspace_Access,
                Permissions.SystemAdministration.Workspace_Access
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All
        };

        var principal = CreateUser(Roles.Applicant);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("all permissions content");

        // Act
        tagHelper.Process(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public void Process_Shows_Content_For_System_Administrator_With_Multiple_Workspace_Permissions()
    {
        // Arrange
        var tagHelper = new PermissionsTagHelper
        {
            Permissions =
            [
                Permissions.MyResearch.Workspace_Access,
                Permissions.Approvals.Workspace_Access,
                Permissions.SystemAdministration.Workspace_Access
            ],
            PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All
        };

        var principal = CreateUser(Roles.SystemAdministrator);
        tagHelper.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("admin content");

        // Act
        tagHelper.Process(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // Process Tests - Status-Based

    [Fact]
    public void Process_Suppresses_Content_When_User_Cannot_Access_Status()
    {
        // Arrange
        var statusValue = ProjectRecordStatus.InDraft;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.ProjectRecord
        };

        var principal = CreateUser(Roles.Sponsor); // Sponsor cannot access "In draft"
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("restricted content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public void Process_Shows_Content_When_User_Can_Access_Status()
    {
        // Arrange
        var statusValue = ProjectRecordStatus.InDraft;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.ProjectRecord
        };

        var principal = CreateUser(Roles.Applicant); // Applicant can access "In draft"
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("visible content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public void Process_Suppresses_Content_When_User_Has_Permission_But_Cannot_Access_Status()
    {
        // Arrange
        var statusValue = ProjectRecordStatus.InDraft;
        var modelExpression = CreateModelExpression(statusValue);

        var tagHelper = new PermissionsTagHelper
        {
            Permission = Permissions.MyResearch.ProjectRecord_Read,
            StatusFor = modelExpression,
            StatusEntity = StatusEntitiy.ProjectRecord
        };

        var principal = CreateUser(Roles.Sponsor); // Sponsor has read permission but cannot access "In draft"
        var http = new DefaultHttpContext { User = principal };
        tagHelper.ViewContext = new ViewContext { HttpContext = http };

        var output = new TagHelperOutput(
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("restricted content");

        var context = new TagHelperContext([], new Dictionary<object, object?>(), "test");

        // Act
        tagHelper.Process(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
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

    private static ModelExpression CreateModelExpression(string statusValue)
    {
        // Create a simple model metadata for a string property
        var modelMetadata = new EmptyModelMetadataProvider()
            .GetMetadataForType(typeof(string));

        // Create ModelExpression with the status value as the model
        return new ModelExpression(
            name: "Status",
            modelExplorer: new ModelExplorer(
                metadataProvider: new EmptyModelMetadataProvider(),
                container: null!,
                metadata: modelMetadata,
                model: statusValue
            )
        );
    }
}