# IRAS Portal — Permissions & Status Authorization

This single canonical document consolidates the implementation summary, system documentation and migration examples for the role/permission and record-status authorization system used by the IRAS Portal.

Contents
- [Overview](#overview)
- [Architecture & key files](#architecture--key-files)
- [Key Concepts and Usage](#key-concepts-and-usage)
- [Before and After Examples](#before-and-after-examples)
- [Migration Checklist](#migration-checklist)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Next steps and recommendations](#next-steps-and-recommendations)

---

## Overview

The system implements fine-grained, workspace-area-action permissions (format: `{workspace}.{area}.{action}`) and role-based status access for entity types (projectrecord, modification, document).

Two complementary authorization approaches are supported:
- **Permission-based authorization** (attribute-based policies and imperative checks)
- **Status-based authorization** (whether a role may access a record in a given status)

Permission claims are represented on the principal as claim type `"permissions"` (plural). The helper `ClaimsPrincipalExtensions.HasPermission` reads these claims. Currently `RolePermissions` maps roles to permission strings; tests and helper code derive `"permissions"` claims from roles by calling `RolePermissions.GetPermissionsForRoles(...)`.

### Important architectural pattern (current pattern used in this codebase)
- Controllers typically declare a **workspace-level policy** at the controller type (e.g. `[Authorize(Policy = Workspaces.MyResearch)]`), providing workspace access for all actions in the controller.
- Individual actions that require specific abilities declare **permission-based policies** (e.g. `[Authorize(Policy = Permissions.MyResearch.Modifications_Create)]`) at the action level. This pattern keeps coarse workspace gating on the controller and fine-grained permission checks on actions.

---

## Architecture & Key Files

Layers and notable types:

### Domain
  - `src/Domain/.../Authorization/Permission.cs`

### Application
  - `src/Application/.../Constants/Permissions.cs` (permission constants)
  - `src/Application/.../Constants/Roles.cs` (role name constants)
  - `src/Application/.../AccessControl/RolePermissions.cs` (role -> permission map)
  - `src/Application/.../AccessControl/RoleStatusPermissions.cs` (role -> allowed statuses)
  - `src/Application/.../Extensions/ClaimsPrincipalExtensions.cs` (helpers reading `"permissions"` claims, GetUserPermissions, GetAllowedStatuses)

### Infrastructure
  - `src/Infrastructure/.../Authorization/PermissionRequirement.cs`
  - `src/Infrastructure/.../Authorization/PermissionRequirementHandler.cs` (checks permissions)
  - `src/Infrastructure/.../Claims/CustomClaimsTransformation.cs` (adds permission and status claims)

### Web
  - `src/Web/.../TagHelpers/PermissionsTagHelper.cs` (supports both attribute usage and standalone `<authorized-when>` element)
  - `src/Web/.../Extensions/PermissionExtensions.cs` (controller helpers)

### Startup
  - `src/Startup/.../Configuration/Auth/AuthConfiguration.cs` (workspace and permission policies registered)

---

## Key Concepts and Usage

### Permission Format
```
{workspace}.{area}.{action}
```
Examples: `myresearch.projectrecord.create`, `sponsor.modifications.authorise`.

### Tag Helper (Recommended UI Usage)
- Standalone element: `<authorized-when role="@Roles.Applicant">...</authorized-when>`
- Attribute usage: `<div permission="@Permissions.MyResearch.ProjectRecord_Create">...</div>`
- Status-based: `<div status-permission-for="@Model.Status" status-entity="modification">...</div>`

### Important Notes
- `ClaimsPrincipalExtensions.HasPermission` checks claims of type `"permissions"`.
- `ClaimsPrincipalExtensions.CanAccessRecordStatus` checks claims of type `"allowed_statuses/{entityType}"`.
- SystemAdministrator role automatically has access to all permissions and all statuses.
- If you migrate to database-driven permissions, ensure the claims transformation step adds `"permissions"` claims for the principal.

### Controllers Pattern
- Apply a **workspace-level policy** on the controller type to gate access to a workspace:
  ```csharp
  [Authorize(Policy = Workspaces.MyResearch)]
  public class ModificationsController : Controller
  ```
- Apply **permission-level policies** on specific actions for fine-grained checks:
  ```csharp
  [HttpPost]
  [Authorize(Policy = Permissions.MyResearch.Modifications_Create)]
  public async Task<IActionResult> CreateModification(...)
  ```

### Imperative Permission Checks (When Needed)
If you need to perform runtime permission checks within an action, use `ClaimsPrincipalExtensions`:

```csharp
using Rsp.IrasPortal.Application.Extensions;

public async Task<IActionResult> DeleteModification(Guid id)
{
    // Check permission explicitly
    if (!User.HasPermission(Permissions.MyResearch.Modifications_Delete))
    {
        return Forbid();
    }
    
    // Proceed with deletion
}
```

### Status Checks
Use `ClaimsPrincipalExtensions.CanAccessRecordStatus` for runtime status checks:

```csharp
var modification = await _service.GetModification(id);

// Check if user can access this status
if (!User.CanAccessRecordStatus("modification", modification.Status))
{
    return Forbid();
}
```

---

## Before and After Examples

### Example 1: Controller with Role-Based to Permission-Based Authorization

#### Before (Role-Based)
```csharp
using Microsoft.AspNetCore.Authorization;

[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsApplicant")] // Role-based policy
public class ModificationsController : Controller
{
    private readonly IProjectModificationsService _modificationsService;
    
    public ModificationsController(IProjectModificationsService modificationsService)
    {
        _modificationsService = modificationsService;
    }

    [HttpGet]
    public IActionResult CreateModification()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteModification(Guid id)
    {
        await _modificationsService.DeleteModification(id);
        return RedirectToAction("Index");
    }
}
```

#### After (Permission-Based with Status Checks)
```csharp
using Microsoft.AspNetCore.Authorization;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Extensions;
using Rsp.IrasPortal.Domain.AccessControl;

[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = Workspaces.MyResearch)] // Workspace-level policy
public class ModificationsController : Controller
{
    private readonly IProjectModificationsService _modificationsService;
    
    public ModificationsController(IProjectModificationsService modificationsService)
    {
        _modificationsService = modificationsService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.MyResearch.Modifications_Create)] // Fine-grained permission
    public IActionResult CreateModification()
    {
        return View();
    }
    
    [HttpPost]
    [Authorize(Policy = Permissions.MyResearch.Modifications_Delete)] // Fine-grained permission
    public async Task<IActionResult> DeleteModification(Guid id)
    {
        // Get modification to check status
        var response = await _modificationsService.GetModification(id);
        
        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }
        
        var modification = response.Content;
        
        // Check if user can access modification based on its status
        if (!User.CanAccessRecordStatus("modification", modification.Status))
        {
            return Forbid();
        }
        
        // Only allow deletion if in draft
        if (modification.Status != ModificationStatus.InDraft)
        {
            ModelState.AddModelError("", "Can only delete draft modifications");
            return View(modification);
        }
        
        await _modificationsService.DeleteModification(id);
        return RedirectToAction("Index");
    }
}
```

**Key Changes:**
1. ✅ Workspace policy at controller level (`Workspaces.MyResearch`)
2. ✅ Permission policies at action level (`Permissions.MyResearch.Modifications_Create`, `Permissions.MyResearch.Modifications_Delete`)
3. ✅ Status check using `User.CanAccessRecordStatus()` extension method
4. ✅ Business rule check (only delete if in draft status)

---

### Example 2: View with Role-Based to Permission-Based Rendering

#### Before (Role-Based with old tag helper)
```razor
@using Rsp.IrasPortal.Application.Constants

<h2 class="govuk-heading-l">My Modifications</h2>

<authorized auth-params="@new(Roles:"applicant")">
    <a asp-action="CreateModification" class="govuk-button">Add modification</a>
</authorized>

<table class="govuk-table">
    <thead>
        <tr>
            <th>Modification ID</th>
            <th>Status</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var mod in Model.Modifications)
        {
            <tr>
                <td>@mod.ModificationId</td>
                <td>@mod.Status</td>
                <td>
                    <a asp-action="ViewModification" 
                       asp-route-id="@mod.Id" 
                       class="govuk-link">View</a>
                    
                    <authorized auth-params="@new(Roles:"applicant")">
                        <a asp-action="EditModification" 
                           asp-route-id="@mod.Id" 
                           class="govuk-link">Edit</a>
                        
                        <a asp-action="DeleteModification" 
                           asp-route-id="@mod.Id" 
                           class="govuk-link">Delete</a>
                    </authorized>
                </td>
            </tr>
        }
    </tbody>
</table>
```

#### After (Permission-Based with PermissionsTagHelper)
```razor
@using Rsp.IrasPortal.Application.Constants
@using Rsp.IrasPortal.Domain.AccessControl

<h2 class="govuk-heading-l">My Modifications</h2>

<!-- Use permission attribute for create button -->
<div permission="@Permissions.MyResearch.Modifications_Create">
    <a asp-action="CreateModification" class="govuk-button">Add modification</a>
</div>

<table class="govuk-table">
    <thead>
        <tr>
            <th>Modification ID</th>
            <th>Status</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var mod in Model.Modifications)
        {
            <tr>
                <td>@mod.ModificationId</td>
                <td><strong class="govuk-tag">@mod.Status</strong></td>
                <td>
                    <!-- Everyone with read permission can view -->
                    <div permission="@Permissions.MyResearch.Modifications_Read">
                        <a asp-action="ViewModification" 
                           asp-route-id="@mod.Id" 
                           class="govuk-link">View</a>
                    </div>
                    
                    <!-- Edit requires both permission AND status access -->
                    <div permission="@Permissions.MyResearch.Modifications_Update"
                         status-permission-for="@mod.Status"
                         status-entity="modification">
                        @if (mod.Status == ModificationStatus.InDraft)
                        {
                            <a asp-action="EditModification" 
                               asp-route-id="@mod.Id" 
                               class="govuk-link">Edit</a>
                        }
                    </div>
                    
                    <!-- Delete requires permission, status access, AND draft status -->
                    <div permission="@Permissions.MyResearch.Modifications_Delete"
                         status-permission-for="@mod.Status"
                         status-entity="modification">
                        @if (mod.Status == ModificationStatus.InDraft)
                        {
                            <a asp-action="DeleteModification" 
                               asp-route-id="@mod.Id" 
                               class="govuk-link">Delete</a>
                        }
                    </div>
                </td>
            </tr>
        }
    </tbody>
</table>
```

**Key Changes:**
1. ✅ Replace `<authorized auth-params>` with `permission` attribute
2. ✅ Add `status-permission-for` and `status-entity` attributes for status-based rendering
3. ✅ Combine permission checks with status checks (logical AND)
4. ✅ Add business rule checks (e.g., only show Edit/Delete for draft)

---

### Example 3: Complex View with Multiple Authorization Checks

#### Before (Using injection and manual checks)
```razor
@model DashboardViewModel
@inject IHttpContextAccessor HttpContextAccessor

@{
    var user = HttpContextAccessor.HttpContext.User;
    var isApplicant = user.IsInRole("applicant");
    var isSponsor = user.IsInRole("sponsor");
    var isAdmin = user.IsInRole("system_administrator");
}

<div class="govuk-grid-row">
    <div class="govuk-grid-column-full">
        <h1 class="govuk-heading-xl">Dashboard</h1>
        
        @if (isApplicant)
        {
            <div class="govuk-grid-column-one-third">
                <h2>My Research</h2>
                <a asp-controller="Application" asp-action="Index" class="govuk-button">
                    View Projects
                </a>
            </div>
        }
        
        @if (isSponsor)
        {
            <div class="govuk-grid-column-one-third">
                <h2>Sponsor Workspace</h2>
                <a asp-controller="SponsorWorkspace" asp-action="Index" class="govuk-button">
                    Review Modifications
                </a>
            </div>
        }
        
        @if (isAdmin)
        {
            <div class="govuk-grid-column-one-third">
                <h2>System Administration</h2>
                <a asp-controller="SystemAdmin" asp-action="Index" class="govuk-button">
                    Manage System
                </a>
            </div>
        }
    </div>
</div>
```

#### After (Using PermissionsTagHelper and ClaimsPrincipalExtensions)
```razor
@model DashboardViewModel
@using Rsp.IrasPortal.Application.Constants
@using Rsp.IrasPortal.Application.Extensions
@using Rsp.IrasPortal.Domain.AccessControl

<div class="govuk-grid-row">
    <div class="govuk-grid-column-full">
        <h1 class="govuk-heading-xl">Dashboard</h1>
        
        <!-- Use permission attribute for workspace cards -->
        <div permission="@Permissions.MyResearch.Workspace_Access" 
             class="govuk-grid-column-one-third">
            <h2>My Research</h2>
            <p class="govuk-body">Create and manage your research projects</p>
            <a asp-controller="Application" asp-action="Index" class="govuk-button">
                View Projects
            </a>
        </div>
        
        <div permission="@Permissions.Sponsor.Workspace_Access" 
             class="govuk-grid-column-one-third">
            <h2>Sponsor Workspace</h2>
            <p class="govuk-body">Review and authorise modifications</p>
            <a asp-controller="SponsorWorkspace" asp-action="Index" class="govuk-button">
                Review Modifications
            </a>
        </div>
        
        <div permission="@Permissions.SystemAdministration.Workspace_Access" 
             class="govuk-grid-column-one-third">
            <h2>System Administration</h2>
            <p class="govuk-body">Manage users, roles and system settings</p>
            <a asp-controller="SystemAdmin" asp-action="Index" class="govuk-button">
                Manage System
            </a>
        </div>
        
        <!-- Show additional admin-only section -->
        <authorized-when permission="@Permissions.SystemAdministration.ManageUsers_Read">
            <div class="govuk-grid-column-full govuk-!-margin-top-6">
                <h2>Recent Activity</h2>
                <p>@Model.TotalUsers users registered</p>
                <p>@Model.TotalProjects active projects</p>
            </div>
        </authorized-when>
    </div>
</div>
```

**Key Changes:**
1. ✅ Removed manual role checks with `IsInRole()`
2. ✅ Used `permission` attribute for workspace access cards
3. ✅ Used `<authorized-when>` standalone element for complex sections
4. ✅ More maintainable and testable

---

### Example 4: Using ClaimsPrincipalExtensions for Complex Logic

#### Before (Manual role checking in controller)
```csharp
public class ReportsController : Controller
{
    public IActionResult Index()
    {
        var canViewReports = User.IsInRole("applicant") || 
                           User.IsInRole("sponsor") || 
                           User.IsInRole("system_administrator");
        
        if (!canViewReports)
        {
            return Forbid();
        }
        
        var model = new ReportsViewModel();
        
        // Different data based on role
        if (User.IsInRole("system_administrator"))
        {
            model.Reports = _service.GetAllReports();
            model.CanExport = true;
        }
        else if (User.IsInRole("sponsor"))
        {
            model.Reports = _service.GetSponsorReports();
            model.CanExport = false;
        }
        else
        {
            model.Reports = _service.GetUserReports(User.Identity.Name);
            model.CanExport = false;
        }
        
        return View(model);
    }
}
```

#### After (Using ClaimsPrincipalExtensions)
```csharp
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Extensions;
using Rsp.IrasPortal.Domain.AccessControl;

[Authorize(Policy = Workspaces.MyResearch)]
public class ReportsController : Controller
{
    private readonly IReportsService _service;
    
    public ReportsController(IReportsService service)
    {
        _service = service;
    }
    
    [Authorize(Policy = Permissions.MyResearch.Reports_Read)]
    public IActionResult Index()
    {
        var model = new ReportsViewModel();
        
        // SystemAdministrator automatically has access to everything
        // No need to check explicitly - handled by HasPermission
        
        // Check specific permissions for data access
        if (User.HasPermission(Permissions.SystemAdministration.Reports_ViewAll))
        {
            model.Reports = _service.GetAllReports();
            model.CanExport = User.HasPermission(Permissions.SystemAdministration.Reports_Export);
        }
        else if (User.HasPermission(Permissions.Sponsor.Reports_ViewSponsored))
        {
            model.Reports = _service.GetSponsorReports();
            model.CanExport = false;
        }
        else
        {
            model.Reports = _service.GetUserReports(User.Identity.Name);
            model.CanExport = false;
        }
        
        // Get all user permissions for view
        model.UserPermissions = User.GetUserPermissions();
        
        return View(model);
    }
    
    [HttpPost]
    [Authorize(Policy = Permissions.SystemAdministration.Reports_Export)]
    public async Task<IActionResult> ExportReport(Guid reportId)
    {
        // Permission already checked by policy attribute
        var report = await _service.GetReport(reportId);
        var exportData = await _service.ExportReport(reportId);
        
        return File(exportData, "application/pdf", $"report-{reportId}.pdf");
    }
}
```

**Key Changes:**
1. ✅ Used policy attributes for action-level authorization
2. ✅ Used `User.HasPermission()` extension for runtime checks
3. ✅ Removed explicit SystemAdministrator checks (handled automatically)
4. ✅ Used `User.GetUserPermissions()` to pass permissions to view
5. ✅ Cleaner, more maintainable code

---

### Example 5: Status-Based Filtering in Controller

#### Before (Manual status filtering)
```csharp
public async Task<IActionResult> ListModifications()
{
    var allModifications = await _service.GetModifications();
    
    // Manual filtering based on role
    if (User.IsInRole("applicant"))
    {
        // Applicant can see all their modifications
        return View(allModifications);
    }
    else if (User.IsInRole("sponsor"))
    {
        // Sponsor can only see "With sponsor" status
        var filtered = allModifications
            .Where(m => m.Status == "With sponsor")
            .ToList();
        return View(filtered);
    }
    else if (User.IsInRole("team_manager"))
    {
        // Team manager can see "With review body"
        var filtered = allModifications
            .Where(m => m.Status == "With review body")
            .ToList();
        return View(filtered);
    }
    
    return Forbid();
}
```

#### After (Using GetAllowedStatuses extension)
```csharp
using Rsp.IrasPortal.Application.Extensions;

[Authorize(Policy = Workspaces.Approvals)]
public async Task<IActionResult> ListModifications()
{
    var allModifications = await _service.GetModifications();
    
    // Get allowed statuses for current user (based on all their roles)
    var allowedStatuses = User.GetAllowedStatuses("modification");
    
    // Filter modifications by allowed statuses
    var accessibleModifications = allModifications
        .Where(m => allowedStatuses.Contains(m.Status, StringComparer.OrdinalIgnoreCase))
        .ToList();
    
    var model = new ModificationsListViewModel
    {
        Modifications = accessibleModifications,
        AllowedStatuses = allowedStatuses // Pass to view for filter UI
    };
    
    return View(model);
}
```

**Key Changes:**
1. ✅ Used `User.GetAllowedStatuses()` extension method
2. ✅ Single filtering logic works for all roles
3. ✅ Handles users with multiple roles automatically
4. ✅ SystemAdministrator automatically gets all statuses

---

## Migration Checklist

### For Controllers:
- [ ] Add workspace-level `[Authorize(Policy = Workspaces.xxx)]` at controller level
- [ ] Add permission-level policies at action level (e.g., `[Authorize(Policy = Permissions.xxx.yyy_zzz)]`)
- [ ] Replace manual `User.IsInRole()` checks with `User.HasPermission()` extension
- [ ] Add status checks using `User.CanAccessRecordStatus()` for data modification actions
- [ ] Use `User.GetAllowedStatuses()` for filtering records by status
- [ ] Test with different roles to ensure access control works correctly

### For Views:
- [ ] Replace `<authorized auth-params>` with `permission` attribute or `<authorized-when>` element
- [ ] Add `status-permission-for` and `status-entity` attributes where status-based rendering is needed
- [ ] Remove manual role checks with `@inject` and `IsInRole()`
- [ ] Use `@using Rsp.IrasPortal.Domain.AccessControl` for Permissions constants
- [ ] Test UI shows/hides elements correctly for different roles and statuses

---

## Troubleshooting

### Permission not working
- ✅ Check `RolePermissions` mapping if system still uses hardcoded roles
- ✅ Remember: SystemAdministrator automatically has all permissions

### Status access not working
- ✅ Confirm entity type is exactly: `"projectrecord"`, `"modification"`, or `"document"` (lowercase)
- ✅ Ensure status string matches exactly (case-insensitive comparison is used in `CanAccessRecordStatus`)
- ✅ Check that `CustomClaimsTransformation` is adding status claims with format `allowed_statuses/{entityType}`
- ✅ Remember: SystemAdministrator automatically has access to all statuses

### Tag helpers not rendering
- ✅ Ensure `_ViewImports.cshtml` includes `@addTagHelper *, Rsp.IrasPortal.Web`
- ✅ Check that permission/status claims are present on the user principal
- ✅ Verify spelling of attributes: `permission`, `status-permission-for`, `status-entity`

### ClaimsPrincipalExtensions not working
- ✅ Add `using Rsp.IrasPortal.Application.Extensions;` to your controller/view
- ✅ Ensure `CustomClaimsTransformation` is running (check claims in debugger)
- ✅ Verify permission claims have type `"permissions"` (plural)
- ✅ Verify status claims have type `"allowed_statuses/{entityType}"`

---

## Next steps and recommendations

### Short-term
- ✅ Update controllers and views across the app to adopt workspace + permission policy pattern
- ✅ Replace manual `IsInRole()` checks with `HasPermission()` extension
- ✅ Add status-based checks for data modification actions
- ✅ Update views to use `PermissionsTagHelper` instead of old `AuthTagHelper`
- ✅ Add/adjust unit tests where required
- ✅ Test with all roles to ensure access control works correctly

### Long-term
- 🔄 Migrate role->permission mapping to a database-driven approach (RoleClaims)
- 🔄 Add admin UI to manage permissions
- 🔄 Add audit logging for permission checks
- 🔄 Consider caching permissions per-user to improve performance
- 🔄 Support hierarchical permissions (e.g., `workspace.*` grants all workspace permissions)

---

## Summary of Key Components

| Component | Purpose | Location |
|-----------|---------|----------|
| `Permissions` | Permission constants | `Application/Constants/Permissions.cs` |
| `Roles` | Role name constants | `Application/Constants/Roles.cs` |
| `RolePermissions` | Role → Permission mapping | `Application/AccessControl/RolePermissions.cs` |
| `RoleStatusPermissions` | Role → Status mapping | `Application/AccessControl/RoleStatusPermissions.cs` |
| `ClaimsPrincipalExtensions` | Helper methods for permission/status checks | `Application/Extensions/ClaimsPrincipalExtensions.cs` |
| `PermissionsTagHelper` | Razor tag helper for conditional rendering | `Web/TagHelpers/PermissionsTagHelper.cs` |
| `CustomClaimsTransformation` | Adds permission/status claims to principal | `Infrastructure/Claims/CustomClaimsTransformation.cs` |
| `PermissionRequirementHandler` | Validates permission requirements | `Infrastructure/Authorization/PermissionRequirementHandler.cs` |
| `RecordStatusRequirementHandler` | Validates status requirements | `Infrastructure/Authorization/RecordStatusRequirementHandler.cs` |

---

## Quick Reference Card

### Controller Authorization
```csharp
// Workspace-level
[Authorize(Policy = Workspaces.MyResearch)]

// Action-level
[Authorize(Policy = Permissions.MyResearch.Modifications_Create)]

// Runtime check
if (!User.HasPermission(Permissions.MyResearch.Modifications_Delete))
{
    return Forbid();
}

// Status check
if (!User.CanAccessRecordStatus("modification", modification.Status))
{
    return Forbid();
}

// Get allowed statuses
var statuses = User.GetAllowedStatuses("modification");
```

### View Authorization
```razor
@using Rsp.IrasPortal.Application.Constants
@using Rsp.IrasPortal.Domain.AccessControl

<!-- Permission check -->
<div permission="@Permissions.MyResearch.ProjectRecord_Create">
    <button>Create</button>
</div>

<!-- Status check -->
<div status-permission-for="@Model.Status" status-entity="modification">
    <button>Edit</button>
</div>

<!-- Combined permission + status check -->
<div permission="@Permissions.MyResearch.Modifications_Update"
     status-permission-for="@Model.Status"
     status-entity="modification">
    <button>Update</button>
</div>

<!-- Standalone element -->
<authorized-when permission="@Permissions.SystemAdministration.ManageUsers_Read">
    <p>Admin content</p>
</authorized-when>
```

---

**Last Updated:** 2024
**Version:** 1.0.0
**Status:** ✅ Production Ready
