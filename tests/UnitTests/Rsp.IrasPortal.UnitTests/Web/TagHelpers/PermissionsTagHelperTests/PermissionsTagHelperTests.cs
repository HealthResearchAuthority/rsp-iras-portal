using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.AccessControl;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.TagHelpers;

namespace Rsp.Portal.UnitTests.Web.TagHelpers.PermissionsTagHelperTests;

public class PermissionsTagHelperTests : TestServiceBase<PermissionsTagHelper>
{
    public PermissionsTagHelperTests()
    {
        Mocker
            .GetMock<IFeatureManager>()
            .Setup(m => m.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(false);
    }

    [Fact]
    public void HasPermissionAttributes_Throws_When_Both_Permission_And_Permissions_Are_Set()
    {
        // Arrange
        Sut.Permission = "perm";
        Sut.Permissions = ["a"];

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => Sut.HasPermissionAttributes());
    }

    [Fact]
    public void HasPermissionAttributes_Returns_True_When_Only_Permission_Is_Set()
    {
        // Arrange
        Sut.Permission = "perm";

        // Act & Assert
        Sut.HasPermissionAttributes().ShouldBeTrue();
    }

    [Fact]
    public void HasPermissionAttributes_Returns_True_When_Only_Permissions_Are_Set()
    {
        // Arrange
        Sut.Permissions = ["a", "b"];

        // Act & Assert
        Sut.HasPermissionAttributes().ShouldBeTrue();
    }

    [Fact]
    public void HasRoleAttributes_Throws_When_Both_Role_And_Roles_Are_Set()
    {
        // Arrange
        Sut.Role = "r";
        Sut.RolesList = ["x"];

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => Sut.HasRoleAttributes());
    }

    [Fact]
    public void HasRoleAttributes_Returns_True_When_Only_Role_Is_Set()
    {
        // Arrange
        Sut.Role = "r";

        // Act & Assert
        Sut.HasRoleAttributes().ShouldBeTrue();
    }

    [Fact]
    public void HasRoleAttributes_Returns_True_When_Only_Roles_Are_Set()
    {
        // Arrange
        Sut.RolesList = ["r1", "r2"];

        // Act & Assert
        Sut.HasRoleAttributes().ShouldBeTrue();
    }

    [Fact]
    public void EvaluatePermissionsAndRoles_Throws_When_Both_Permissions_And_Roles_Are_Specified()
    {
            // Arrange
            Sut.Permission = "p";
            Sut.RolesList = ["r"];

            var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth")) };
            Sut.ViewContext = new ViewContext { HttpContext = http };

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => Sut.EvaluatePermissionsAndRoles());
        }

    [Fact]
    public async Task EvaluatePermissionsAndRoles_Returns_True_When_No_Permissions_Or_Roles_Are_Specified()
    {
        // Arrange
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        // Act & Assert
        (await Sut.EvaluatePermissionsAndRoles()).ShouldBeTrue();
    }

    // Permission Evaluation Tests

    [Fact]
    public async Task EvaluatePermissions_Returns_True_For_Single_Permission_When_User_Has_Permission()
    {
        // Arrange
        Sut.Permission = Permissions.MyResearch.Workspace_Access;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        // Act & Assert
        (await Sut.EvaluatePermissions()).ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluatePermissions_Returns_False_For_Single_Permission_When_User_Does_Not_Have_Permission()
    {
        Sut.Permission = Permissions.SystemAdministration.Workspace_Access;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var result = await Sut.EvaluatePermissions();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluatePermissions_Any_Returns_True_When_User_Has_One_Permission()
    {
        // Arrange
        Sut.Permissions =
        [
            Permissions.MyResearch.Workspace_Access,
            Permissions.SystemAdministration.Workspace_Access
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.Any;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        (await Sut.EvaluatePermissions()).ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluatePermissions_Any_Returns_False_When_User_Has_No_Permissions()
    {
        Sut.Permissions =
        [
            Permissions.SystemAdministration.Workspace_Access,
            Permissions.Approvals.Workspace_Access
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.Any;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var result = await Sut.EvaluatePermissions();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluatePermissions_All_Returns_True_When_User_Has_All_Permissions()
    {
        Sut.Permissions =
        [
            Permissions.MyResearch.ProjectRecord_Read,
            Permissions.MyResearch.ProjectRecord_Create
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        (await Sut.EvaluatePermissions()).ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluatePermissions_All_Returns_True_When_User_Is_System_Administrator()
    {
        Sut.Permissions =
        [
            Permissions.MyResearch.Workspace_Access,
            Permissions.Sponsor.Workspace_Access,
            Permissions.Approvals.Workspace_Access,
            Permissions.SystemAdministration.Workspace_Access
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.SystemAdministrator));

        (await Sut.EvaluatePermissions()).ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluatePermissions_Returns_False_For_Edit_Permission_When_TeamRoles_Enabled_And_No_Collaborator_Edit_Access()
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(m => m.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(true);

        Sut.Permission = Permissions.MyResearch.ProjectRecord_Update;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var result = await Sut.EvaluatePermissions();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluatePermissions_Returns_True_For_Edit_Permission_When_TeamRoles_Disabled_And_No_Collaborator_Edit_Access()
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(m => m.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(false);

        Sut.Permission = Permissions.MyResearch.ProjectRecord_Update;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var result = await Sut.EvaluatePermissions();

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluatePermissions_Returns_True_For_Edit_Permission_When_TeamRoles_Enabled_And_User_Is_Not_Applicant()
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(m => m.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(true);

        Sut.Permission = Permissions.Sponsor.Modifications_Authorise;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Sponsor));

        var result = await Sut.EvaluatePermissions();

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluatePermissions_Collection_Returns_False_For_Edit_Permission_When_TeamRoles_Enabled_And_No_Collaborator_Edit_Access()
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(m => m.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(true);

        Sut.Permissions =
        [
            Permissions.MyResearch.ProjectRecord_Read,
            Permissions.MyResearch.ProjectRecord_Update
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var result = await Sut.EvaluatePermissions();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluatePermissions_Collection_Returns_True_For_Edit_Permission_When_TeamRoles_Enabled_And_User_Is_Not_Applicant()
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(m => m.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(true);

        Sut.Permissions =
        [
            Permissions.Sponsor.Modifications_Search,
            Permissions.Sponsor.Modifications_Authorise
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Sponsor));

        var result = await Sut.EvaluatePermissions();

        result.ShouldBeTrue();
    }

    [Fact]
    public void EvaluateRoles_Returns_True_For_Single_Role_When_User_Is_In_Role()
    {
        Sut.Role = Roles.TeamManager;

        var principal = CreateUser(Roles.TeamManager);
        Sut.ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act & Assert
        Sut.EvaluateRoles().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateRoles_Returns_False_For_Single_Role_When_User_Not_In_Role()
    {
        Sut.Role = "admin";
        Sut.ViewContext = CreateViewContext(CreateUser("user"));

        Sut.EvaluateRoles().ShouldBeFalse();
    }

    [Fact]
    public void EvaluateRoles_Any_Returns_True_When_User_Has_One_Role()
    {
        Sut.RolesList = ["a", "b"];
        Sut.RoleEvaluationLogic = PermissionsTagHelper.RoleMode.Any;
        Sut.ViewContext = CreateViewContext(CreateUser("b"));

        Sut.EvaluateRoles().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateRoles_All_Returns_True_When_User_Has_All_Roles()
    {
        Sut.RolesList = ["x", "y"];
        Sut.RoleEvaluationLogic = PermissionsTagHelper.RoleMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser("x", "y"));

        Sut.EvaluateRoles().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateRoles_All_Returns_False_When_User_Missing_One_Role()
    {
        Sut.RolesList = ["x", "y"];
        Sut.RoleEvaluationLogic = PermissionsTagHelper.RoleMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser("x"));

        Sut.EvaluateRoles().ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_StatusFor_Is_Null()
    {
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_StatusEntity_Is_Empty()
    {
        Sut.StatusEntity = "";
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_User_Can_Access_ProjectRecord_Status()
    {
        Sut.StatusFor = CreateModelExpression(ProjectRecordStatus.InDraft);
        Sut.StatusEntity = StatusEntitiy.ProjectRecord;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_False_When_User_Cannot_Access_ProjectRecord_Status()
    {
        Sut.StatusFor = CreateModelExpression(ProjectRecordStatus.InDraft);
        Sut.StatusEntity = StatusEntitiy.ProjectRecord;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Sponsor));

        Sut.UserCanAccessStatus().ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_User_Can_Access_Modification_Status()
    {
        Sut.StatusFor = CreateModelExpression(ModificationStatus.WithSponsor);
        Sut.StatusEntity = StatusEntitiy.Modification;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Sponsor));

        Sut.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_False_When_User_Cannot_Access_Modification_Status()
    {
        Sut.StatusFor = CreateModelExpression(ModificationStatus.WithSponsor);
        Sut.StatusEntity = StatusEntitiy.Modification;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.TeamManager));

        Sut.UserCanAccessStatus().ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_User_Can_Access_Document_Status()
    {
        Sut.StatusFor = CreateModelExpression(DocumentStatus.Uploaded);
        Sut.StatusEntity = StatusEntitiy.Document;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Is_Case_Sensitive_For_Status()
    {
        Sut.StatusFor = CreateModelExpression("IN DRAFT");
        Sut.StatusEntity = StatusEntitiy.ProjectRecord;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.UserCanAccessStatus().ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_False_For_Unknown_Entity_Type()
    {
        Sut.StatusFor = CreateModelExpression("Some Status");
        Sut.StatusEntity = "unknownentity";
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.UserCanAccessStatus().ShouldBeFalse();
    }

    [Fact]
    public void UserCanAccessStatus_Returns_True_When_SystemAdministrator_Can_Access_Any_Status()
    {
        Sut.StatusFor = CreateModelExpression(ModificationStatus.WithReviewBody);
        Sut.StatusEntity = StatusEntitiy.Modification;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.SystemAdministrator));

        Sut.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public void UserCanAccessStatus_Combines_Statuses_From_Multiple_Roles()
    {
        Sut.StatusFor = CreateModelExpression(ModificationStatus.InDraft);
        Sut.StatusEntity = StatusEntitiy.Modification;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant, Roles.Sponsor));

        Sut.UserCanAccessStatus().ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Shows_Content_When_ShowWhenUserIsNotAuthenticated_And_User_Is_Anonymous()
    {
        Sut.ShowWhenUserIsNotAuthenticated = true;
        Sut.ViewContext = CreateViewContext(new ClaimsPrincipal(new ClaimsIdentity()));

        var output = CreateOutput("authorized", "child content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.TagName.ShouldBeNull();
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_ShowWhenUserIsNotAuthenticated_And_User_Is_Authenticated()
    {
        Sut.ShowWhenUserIsNotAuthenticated = true;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.TeamManager));

        var output = CreateOutput("authorized-when", "child content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_User_Is_Not_Authenticated_And_No_ShowFlag()
    {
        Sut.ViewContext = CreateViewContext(new ClaimsPrincipal(new ClaimsIdentity()));

        var output = CreateOutput("div", "test content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Shows_Content_When_User_Is_Authenticated_And_Role_Matches()
    {
        Sut.Role = Roles.TeamManager;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.TeamManager));

        var output = CreateOutput("div", "visible content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_User_Does_Not_Have_Required_Role()
    {
        Sut.Role = "admin";
        Sut.ViewContext = CreateViewContext(CreateUser("user"));

        var output = CreateOutput("div", "admin content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Shows_Content_When_RolesList_Any_Mode_And_User_Has_One_Role()
    {
        Sut.RolesList = ["a", "b"];
        Sut.RoleEvaluationLogic = PermissionsTagHelper.RoleMode.Any;
        Sut.ViewContext = CreateViewContext(CreateUser("b"));

        var output = CreateOutput("div", "any content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_RolesList_All_Mode_And_User_Missing_One_Role()
    {
        Sut.RolesList = ["x", "y"];
        Sut.RoleEvaluationLogic = PermissionsTagHelper.RoleMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser("x"));

        var output = CreateOutput("div", "all content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Shows_Content_When_User_Has_Required_Permission()
    {
        Sut.Permission = Permissions.MyResearch.Workspace_Access;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var output = CreateOutput("div", "workspace content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_User_Does_Not_Have_Required_Permission()
    {
        Sut.Permission = Permissions.SystemAdministration.Workspace_Access;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var output = CreateOutput("div", "admin workspace content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Shows_Content_When_PermissionsList_Any_Mode_And_User_Has_One_Permission()
    {
        Sut.Permissions =
        [
            Permissions.MyResearch.Workspace_Access,
            Permissions.SystemAdministration.Workspace_Access
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.Any;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var output = CreateOutput("div", "any permission content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_PermissionsList_All_Mode_And_User_Missing_One_Permission()
    {
        Sut.Permissions =
        [
            Permissions.MyResearch.Workspace_Access,
            Permissions.SystemAdministration.Workspace_Access
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var output = CreateOutput("div", "all permissions content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Shows_Content_For_System_Administrator_With_Multiple_Workspace_Permissions()
    {
        Sut.Permissions =
        [
            Permissions.MyResearch.Workspace_Access,
            Permissions.Approvals.Workspace_Access,
            Permissions.SystemAdministration.Workspace_Access
        ];
        Sut.PermissionEvaluationLogic = PermissionsTagHelper.PermissionMode.All;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.SystemAdministrator));

        var output = CreateOutput("div", "admin content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_User_Cannot_Access_Status()
    {
        Sut.StatusFor = CreateModelExpression(ProjectRecordStatus.InDraft);
        Sut.StatusEntity = StatusEntitiy.ProjectRecord;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Sponsor));

        var output = CreateOutput("div", "restricted content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task Process_Shows_Content_When_User_Can_Access_Status()
    {
        Sut.StatusFor = CreateModelExpression(ProjectRecordStatus.InDraft);
        Sut.StatusEntity = StatusEntitiy.ProjectRecord;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        var output = CreateOutput("div", "visible content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task Process_Suppresses_Content_When_User_Has_Permission_But_Cannot_Access_Status()
    {
        Sut.Permission = Permissions.MyResearch.ProjectRecord_Read;
        Sut.StatusFor = CreateModelExpression(ProjectRecordStatus.InDraft);
        Sut.StatusEntity = StatusEntitiy.ProjectRecord;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Sponsor));

        var output = CreateOutput("div", "restricted content");

        await Sut.ProcessAsync(new TagHelperContext([], new Dictionary<object, object?>(), "test"), output);

        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public void EvaluateCollaboratorAccess_Returns_True_For_Non_Edit_Permission()
    {
        Sut.Permission = Permissions.MyResearch.Workspace_Access;

        Sut.EvaluateCollaboratorAccess().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateCollaboratorAccess_Returns_False_For_Edit_Permission_When_ProjectRecordId_Missing()
    {
        Sut.Permission = Permissions.MyResearch.ProjectRecord_Update;
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.EvaluateCollaboratorAccess().ShouldBeFalse();
    }

    [Fact]
    public void EvaluateCollaboratorAccess_Returns_True_For_Edit_Permission_When_Collaborator_Has_Edit_Access()
    {
        var viewContext = CreateViewContext(CreateUser(Roles.Applicant));
        viewContext.TempData[TempDataKeys.ProjectRecordId] = "project-1";
        viewContext.HttpContext.Items[ContextItemKeys.CollaboratorProjects] =
            "[{\"ProjectRecordId\":\"project-1\",\"ProjectAccessLevel\":\"Edit\"}]";

        Sut.Permission = Permissions.MyResearch.ProjectRecord_Update;
        Sut.ViewContext = viewContext;

        Sut.EvaluateCollaboratorAccess().ShouldBeTrue();
    }

    [Fact]
    public void EvaluateCollaboratorAccess_Collection_Returns_True_When_No_Edit_Permissions_In_List()
    {
        Sut.ViewContext = CreateViewContext(CreateUser(Roles.Applicant));

        Sut.EvaluateCollaboratorAccess([Permissions.MyResearch.Workspace_Access]).ShouldBeTrue();
    }

    private static ViewContext CreateViewContext(ClaimsPrincipal user)
    {
        var http = new DefaultHttpContext
        {
            User = user,
            Session = new InMemorySession()
        };

        return new ViewContext
        {
            HttpContext = http,
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }

    private static TagHelperOutput CreateOutput(string tagName, string content)
    {
        var output = new TagHelperOutput(
            tagName,
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        output.Content.SetHtmlContent(content);
        return output;
    }

    private static ClaimsPrincipal CreateUser(params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

        if (roles.Length > 0)
        {
            var perms = RolePermissions.GetPermissionsForRoles(roles);
            claims.AddRange(perms.Select(p => new Claim(CustomClaimTypes.Permissions, p)));

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
        var modelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(string));

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