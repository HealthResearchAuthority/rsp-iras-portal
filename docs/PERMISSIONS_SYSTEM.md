# Role-Based Permission System Documentation

## Overview

This document describes the comprehensive role-based permission system implemented in the IRAS Portal. The system provides fine-grained access control based on:

1. **Workspace.Area.Action permissions** - Defines what actions users can perform
2. **Role-based status access** - Defines which record statuses users can access

## Architecture

The implementation follows Clean Architecture principles:

### Domain Layer
- `Permission` record - Represents a permission with workspace, area, and action

### Application Layer
- `Permissions` class - Defines all available permissions in the system
- `Roles` class - Defines all role names
- `RolePermissions` class - Maps roles to their permissions
- `RoleStatusPermissions` class - Maps roles to allowed record statuses
- `IPermissionService` - Service interface for permission checking

### Infrastructure Layer
- `PermissionRequirement` - Authorization requirement for permission-based policies
- `PermissionAuthorizationHandler` - Handler that validates permission requirements
- `RecordStatusRequirement` - Authorization requirement for status-based access
- `RecordStatusAuthorizationHandler` - Handler that validates status requirements

### Web Layer
- `PermissionTagHelper` - Razor tag helper for conditional rendering based on permissions
- `RecordStatusTagHelper` - Razor tag helper for conditional rendering based on status access
- `PermissionExtensions` - Extension methods for controllers

## Usage

### 1. Using Permission Policies on Controllers/Actions

#### Attribute-Based Authorization

```csharp
// Require specific permission on the entire controller
[Authorize(Policy = Permissions.MyResearch.ProjectRecord_Create)]
public class ProjectController : Controller
{
    // All actions require the permission
}

// Require permission on specific action
[HttpGet]
[Authorize(Policy = Permissions.MyResearch.Modifications_Update)]
public IActionResult EditModification(Guid id)
{
    // Only users with update permission can access
}
```

### 2. Dynamic Permission Checking in Action Methods

```csharp
public class ProjectController : Controller
{
    private readonly IPermissionService _permissionService;
    
    public ProjectController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }
    
    [HttpPost]
    public IActionResult DeleteProject(string projectRecordId)
    {
        // Check permission dynamically
        if (!_permissionService.HasPermission(User, Permissions.MyResearch.ProjectRecord_Delete))
        {
            return Forbid();
        }
        
        // Or use extension method
        var forbidResult = this.ForbidIfNoPermission(_permissionService, Permissions.MyResearch.ProjectRecord_Delete);
        if (forbidResult != null)
        {
            return forbidResult;
        }
        
        // Proceed with deletion
        return Ok();
    }
}
```

### 3. Status-Based Access Control

```csharp
public class ModificationController : Controller
{
    private readonly IPermissionService _permissionService;
    private readonly IProjectModificationsService _modificationsService;
    
    [HttpGet]
    public async Task<IActionResult> ViewModification(Guid modificationId)
    {
        var modification = await _modificationsService.GetModification(modificationId);
        
        // Check if user can access this modification based on its status
        if (!_permissionService.CanAccessRecordStatus(User, "modification", modification.Status))
        {
            return Forbid();
        }
        
        // Or use extension method
        var forbidResult = this.ForbidIfCannotAccessStatus(
            _permissionService, 
            "modification", 
            modification.Status);
        if (forbidResult != null)
        {
            return forbidResult;
        }
        
        return View(modification);
    }
    
    [HttpGet]
    public IActionResult ListModifications()
    {
        // Get all statuses the current user can access
        var allowedStatuses = _permissionService.GetAllowedStatuses(User, "modification");
        
        // Filter modifications by allowed statuses
        var modifications = GetModifications()
            .Where(m => allowedStatuses.Contains(m.Status))
            .ToList();
            
        return View(modifications);
    }
}
```

### 4. Conditional Rendering in Views

#### Permission-Based Rendering

```razor
@* Show button only if user has create permission *@
<div permission="@Permissions.MyResearch.ProjectRecord_Create">
    <a asp-action="CreateProject" class="govuk-button">Add project</a>
</div>

@* Hide element if user has permission (inverse logic) *@
<div permission="@Permissions.MyResearch.Modifications_Delete" permission-hide-when-has="true">
    <p>You cannot delete modifications</p>
</div>

@* Multiple permission checks *@
<div permission="@Permissions.MyResearch.Modifications_Update">
    <a asp-action="EditModification" asp-route-id="@Model.Id" class="govuk-link">Edit</a>
</div>

<div permission="@Permissions.MyResearch.Modifications_Delete">
    <a asp-action="DeleteModification" asp-route-id="@Model.Id" class="govuk-link">Delete</a>
</div>
```

#### Status-Based Rendering

```razor
@* Show element only if user can access "In draft" status *@
<div record-status="modification:In draft">
    <p>This modification is in draft</p>
</div>

@* Show action buttons based on modification status *@
@if (Model.Status == ModificationStatus.InDraft)
{
    <div record-status="modification:@Model.Status">
        <button type="submit" class="govuk-button">Submit for review</button>
    </div>
}

@* Show different content based on project record status *@
<div record-status="projectrecord:@Model.Status">
    @if (Model.Status == ProjectRecordStatus.Active)
    {
        <strong class="govuk-tag govuk-tag--green">Active</strong>
    }
</div>
```

### 5. Injecting Permission Service in Views

```razor
@inject IPermissionService PermissionService
@using Rsp.IrasPortal.Application.Constants

@{
    var canCreate = PermissionService.HasPermission(User, Permissions.MyResearch.ProjectRecord_Create);
    var canUpdate = PermissionService.HasPermission(User, Permissions.MyResearch.ProjectRecord_Update);
    var canDelete = PermissionService.HasPermission(User, Permissions.MyResearch.ProjectRecord_Delete);
}

@if (canCreate)
{
    <a asp-action="Create" class="govuk-button">Create New</a>
}

@if (canUpdate || canDelete)
{
    <div class="action-buttons">
        @if (canUpdate)
        {
            <a asp-action="Edit" asp-route-id="@Model.Id" class="govuk-button">Edit</a>
        }
        @if (canDelete)
        {
            <a asp-action="Delete" asp-route-id="@Model.Id" class="govuk-button govuk-button--warning">Delete</a>
        }
    </div>
}
```

## Permission Structure

### Permission Format
Permissions follow the format: `{workspace}.{area}.{action}`

Examples:
- `myresearch.projectrecord.create`
- `sponsor.sponsorreview.authorise`
- `systemadmin.manageusers.delete`

### Available Workspaces
- `myresearch` - My Research workspace
- `sponsor` - Sponsor workspace
- `systemadmin` - System Administration workspace
- `approvals` - Approvals workspace
- `cagmembers` - CAG Members workspace
- `membermanagement` - Member Management workspace
- `cat` - CAT workspace
- `recmembers` - REC Members workspace
- `technicalassurance` - Technical Assurance workspace
- `technicalassurancereviewers` - Technical Assurance Reviewers workspace

## Role-Permission Mapping

### Current Role Permissions (Hardcoded in `RolePermissions` class)

#### Applicant Role
- Full CRUD access to My Research workspace (projects, modifications, documents, sites, etc.)

#### Sponsor Role
- Can review and authorise modifications in Sponsor workspace
- Can access backstage area

#### System Administrator Role
- Full access to System Administration workspace (manage users, review bodies, sponsor organisations)
- Read-only access to My Research workspace
- Can view all approvals

#### Workflow Coordinator Role
- Can search and assign/reassign modifications in Approvals workspace

#### Team Manager Role
- Can search and view modifications in Approvals workspace

#### Study-wide Reviewer Role
- Can search, review, and approve modifications in Approvals workspace

## Status-Based Access Control

### Entity Types
- `projectrecord` - Project records
- `modification` - Modifications
- `document` - Documents

### Status Access by Role

#### Project Record Statuses
- **Applicant**: Can access "In draft" and "Active"
- **Sponsor**: Can access "Active" only
- **System Administrator**: Can access all statuses
- **Workflow Coordinator**: Can access "Active" only
- **Team Manager**: Can access "Active" only
- **Study-wide Reviewer**: Can access "Active" only

#### Modification Statuses
- **Applicant**: Can access all statuses
- **Sponsor**: Can access "With sponsor", "Approved", "Not approved"
- **System Administrator**: Can access all statuses
- **Workflow Coordinator**: Can access "With review body", "Approved", "Not approved"
- **Team Manager**: Can access "With review body", "Approved", "Not approved"
- **Study-wide Reviewer**: Can access "With review body", "Approved", "Not approved"

#### Document Statuses
- Similar pattern to modifications based on role

## Future Enhancements

### Moving to Database-Driven Permissions

The current implementation hardcodes permissions in the `RolePermissions` class. In the future, you can:

1. Store permissions in the `RoleClaims` table
2. Load permissions from database during login
3. Add permissions as claims to the user principal
4. Update the `PermissionAuthorizationHandler` to read from claims instead of the static map

Example migration path:

```csharp
// In CustomClaimsTransformation.cs
public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
{
    // Existing code...
    
    // Add permission claims from database
    var userRoles = user.Roles;
    var permissions = await GetPermissionsFromDatabase(userRoles);
    
    foreach (var permission in permissions)
    {
        claimsIdentity.AddClaim(new Claim("permission", permission));
    }
    
    return principal;
}

// Update PermissionAuthorizationHandler to check claims
protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    PermissionRequirement requirement)
{
    var hasPermission = context.User.HasClaim("permission", requirement.Permission);
    
    if (hasPermission)
    {
        context.Succeed(requirement);
    }
    
    return Task.CompletedTask;
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public void HasPermission_WhenUserHasRole_ReturnsTrue()
{
    // Arrange
    var permissionService = new PermissionService();
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, Roles.Applicant)
    };
    var identity = new ClaimsIdentity(claims, "TestAuthType");
    var claimsPrincipal = new ClaimsPrincipal(identity);

    // Act
    var hasPermission = permissionService.HasPermission(
        claimsPrincipal, 
        Permissions.MyResearch.ProjectRecord_Create);

    // Assert
    Assert.True(hasPermission);
}
```

## Troubleshooting

### Permission Not Working

1. Ensure the policy is registered in `AuthConfiguration.ConfigureAuthorization`
2. Verify the user has the required role(s)
3. Check that the role-permission mapping is correct in `RolePermissions`
4. Ensure authorization handlers are registered in DI container

### Status Access Not Working

1. Verify the entity type matches: "projectrecord", "modification", or "document"
2. Check the status string matches exactly (case-insensitive comparison is used)
3. Ensure the role-status mapping is correct in `RoleStatusPermissions`
4. Verify user roles are properly set in claims

### Tag Helpers Not Rendering

1. Ensure tag helpers are registered in `_ViewImports.cshtml`:
   ```razor
   @addTagHelper *, Rsp.IrasPortal.Web
   ```
2. Check that `IPermissionService` is registered in DI
3. Verify `IHttpContextAccessor` is registered

## Best Practices

1. **Use permission constants** - Always use constants from `Permissions` class instead of magic strings
2. **Check permissions early** - Validate permissions at the start of action methods
3. **Layer security** - Use both attribute-based and dynamic checks for defense in depth
4. **Test permissions** - Write unit tests for critical permission logic
5. **Document custom permissions** - If adding new permissions, update this documentation
6. **Consider performance** - Permission checks are fast, but avoid unnecessary repeated checks
7. **Use status filtering** - When querying data, filter by allowed statuses to improve performance
8. **Secure UI and API** - Apply permissions to both UI elements and API endpoints
