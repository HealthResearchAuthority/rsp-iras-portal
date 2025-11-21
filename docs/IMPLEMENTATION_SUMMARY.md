# Role-Based Permission System Implementation Summary

## Overview

A comprehensive permission-based authorization system has been implemented for the IRAS Portal following clean architecture principles. The system provides fine-grained access control with two main components:

1. **Permission-based authorization** - workspace.area.action format (e.g., `myresearch.projectrecord.create`)
2. **Status-based authorization** - Controls access to records based on their status and user roles

## What Was Implemented

### 1. Domain Layer (`src\Domain\Rsp.IrasPortal.Domain`)

#### New Files Created:
- **Authorization\Permission.cs**
  - Record type representing a permission with Workspace, Area, and Action
  - Parse and TryParse methods for permission string manipulation
  - FullPermission property returns formatted string

### 2. Application Layer (`src\Application\Rsp.IrasPortal.Application`)

#### New Files Created:

- **Constants\Permissions.cs**
  - Comprehensive list of all permissions in the system
  - Organized by workspace (MyResearch, Sponsor, SystemAdministration, Approvals)
  - ~50+ permission constants covering all areas and actions
  - Examples:
    - `MyResearch.ProjectRecord_Create = "myresearch.projectrecord.create"`
    - `Sponsor.SponsorReview_Authorise = "sponsor.sponsorreview.authorise"`
    - `SystemAdministration.ManageUsers_Delete = "systemadmin.manageusers.delete"`
    - `Approvals.ReviewModifications_Approve = "approvals.reviewmodifications.approve"`

- **Constants\Roles.cs**
  - Centralized role name constants
  - Includes: SystemAdministrator, Applicant, Sponsor, StudyWideReviewer, TeamManager, WorkflowCoordinator, Reviewer

- **Constants\RoleStatusPermissions.cs**
  - Maps roles to allowed record statuses for different entity types
  - Three entity types supported:
    - ProjectRecord (statuses: In draft, Active)
    - Modification (statuses: In draft, With sponsor, With review body, Approved, Not approved)
    - Document (statuses: Uploaded, Failed, Incomplete, Completed, With regulator, Approved, Not approved)
  - `GetAllowedStatuses()` method aggregates statuses across multiple roles

- **Authorization\RolePermissions.cs**
  - Static mapping of roles to their permissions
  - Currently hardcoded but designed to be moved to RoleClaims table in database later
  - Methods:
    - `GetPermissionsForRole(string role)`
    - `GetPermissionsForRoles(IEnumerable<string> roles)`
    - `HasPermission(string role, string permission)`
    - `HasPermission(IEnumerable<string> roles, string permission)`

- **Services\IPermissionService.cs**
  - Service interface for permission checking
  - `PermissionService` implementation
  - Methods:
    - `HasPermission(ClaimsPrincipal user, string permission)`
    - `GetUserPermissions(ClaimsPrincipal user)`
    - `CanAccessRecordStatus(ClaimsPrincipal user, string entityType, string status)`
    - `GetAllowedStatuses(ClaimsPrincipal user, string entityType)`

### 3. Infrastructure Layer (`src\Infrastructure\Rsp.IrasPortal.Infrastructure`)

#### New Files Created:

- **Authorization\PermissionRequirement.cs**
  - IAuthorizationRequirement implementation for permission-based policies
  - Contains the required permission string

- **Authorization\PermissionAuthorizationHandler.cs**
  - AuthorizationHandler implementation
  - Validates user has required permission based on their roles
  - Extracts roles from user claims and checks against RolePermissions

- **Authorization\RecordStatusRequirement.cs**
  - IAuthorizationRequirement for status-based access
  - Contains entity type and status

- **Authorization\RecordStatusAuthorizationHandler.cs**
  - AuthorizationHandler for status-based access
  - Validates user can access record based on status and their roles
  - Supports projectrecord, modification, and document entity types

### 4. Startup/Configuration Layer (`src\Startup\Rsp.IrasPortal`)

#### Modified Files:

- **Configuration\Auth\AuthConfiguration.cs**
  - Registered authorization handlers (PermissionAuthorizationHandler, RecordStatusAuthorizationHandler)
  - Added permission-based policies using AddPermissionPolicy helper method
  - Policies created for key permissions like:
    - MyResearch: ProjectRecord CRUD, Modifications CRUD, ProjectDetails Update
    - Sponsor: SponsorReview Authorize
    - SystemAdministration: ManageUsers CRUD
    - Approvals: SearchSubmittedRecords, AssignModifications, ReviewModifications
  - Existing role-based policies maintained for backward compatibility

- **Configuration\Dependencies\ServicesConfiguration.cs**
  - Registered `IPermissionService` and `PermissionService` as scoped service

### 5. Web Layer (`src\Web\Rsp.IrasPortal.Web`)

#### New Files Created:

- **TagHelpers\PermissionTagHelper.cs**
  - Razor tag helper for conditional rendering based on permissions
  - Usage: `<div permission="myresearch.projectrecord.create">...</div>`
  - Supports `permission-hide-when-has="true"` for inverse logic
  - Automatically removes custom attributes from rendered output

- **TagHelpers\RecordStatusTagHelper.cs**
  - Razor tag helper for conditional rendering based on status access
  - Usage: `<div record-status="modification:In draft">...</div>`
  - Format: `entityType:status`
  - Validates user can access the specified status

- **Extensions\PermissionExtensions.cs**
  - Extension methods for Controller and ClaimsPrincipal
  - Simplified permission checking in controllers:
    - `HasPermission(IPermissionService, string permission)`
    - `CanAccessRecordStatus(IPermissionService, string entityType, string status)`
    - `ForbidIfNoPermission(IPermissionService, string permission)`
    - `ForbidIfCannotAccessStatus(IPermissionService, string entityType, string status)`
    - `GetUserPermissions(IPermissionService)`
    - `GetAllowedStatuses(IPermissionService, string entityType)`

### 6. Documentation

#### New Files Created:

- **docs\PERMISSIONS_SYSTEM.md**
  - Comprehensive documentation covering:
    - System overview and architecture
    - Usage examples for all scenarios
    - Permission structure and format
    - Role-permission mappings
    - Status-based access control
    - Future enhancements (moving to database)
    - Testing examples
    - Troubleshooting guide
    - Best practices

## Permission Mappings Implemented

### Applicant Role
- **Workspaces:** My Research
- **Permissions:** Full CRUD on projects, modifications, documents, sites, sponsor references
- **Status Access:** 
  - ProjectRecord: In draft, Active
  - Modification: All statuses
  - Document: All statuses

### Sponsor Role
- **Workspaces:** Sponsor
- **Permissions:** Review, authorise modifications; access backstage
- **Status Access:**
  - ProjectRecord: Active
  - Modification: With sponsor, Approved, Not approved
  - Document: With regulator, Approved, Not approved

### System Administrator Role
- **Workspaces:** System Administration, Approvals, My Research (read-only)
- **Permissions:** Manage users, review bodies, sponsor organisations; view all records
- **Status Access:** All statuses for all entity types

### Workflow Coordinator Role
- **Workspaces:** Approvals
- **Permissions:** Search, assign, reassign modifications
- **Status Access:**
  - ProjectRecord: Active
  - Modification: With review body, Approved, Not approved
  - Document: With regulator, Approved, Not approved

### Team Manager Role
- **Workspaces:** Approvals
- **Permissions:** Search and view modifications
- **Status Access:**
  - ProjectRecord: Active
  - Modification: With review body, Approved, Not approved
  - Document: With regulator, Approved, Not approved

### Study-wide Reviewer Role
- **Workspaces:** Approvals
- **Permissions:** Search, review, approve modifications
- **Status Access:**
  - ProjectRecord: Active
  - Modification: With review body, Approved, Not approved
  - Document: With regulator, Approved, Not approved

## Usage Patterns

### 1. Attribute-Based Authorization (Declarative)
```csharp
[Authorize(Policy = Permissions.MyResearch.ProjectRecord_Create)]
public IActionResult CreateProject()
{
    return View();
}
```

### 2. Dynamic Permission Checking (Imperative)
```csharp
public IActionResult DeleteProject(string id)
{
    if (!_permissionService.HasPermission(User, Permissions.MyResearch.ProjectRecord_Delete))
    {
        return Forbid();
    }
    // Proceed with deletion
}
```

### 3. Status-Based Access Control
```csharp
public async Task<IActionResult> ViewModification(Guid id)
{
    var modification = await _modificationsService.GetModification(id);
    
    if (!_permissionService.CanAccessRecordStatus(User, "modification", modification.Status))
    {
        return Forbid();
    }
    
    return View(modification);
}
```

### 4. UI Conditional Rendering
```razor
@* Show button only if user has permission *@
<div permission="@Permissions.MyResearch.ProjectRecord_Create">
    <a asp-action="CreateProject" class="govuk-button">Add project</a>
</div>

@* Show content only if user can access status *@
<div record-status="modification:@Model.Status">
    <p>You can view this modification</p>
</div>
```

## Migration Path to Database-Driven Permissions

The current implementation hardcodes permissions in `RolePermissions` class for simplicity. To move to database-driven permissions:

1. **Create RoleClaims records** - Store permissions in the RoleClaims table
2. **Update CustomClaimsTransformation** - Load permissions from database and add as claims
3. **Modify PermissionAuthorizationHandler** - Check permission claims instead of static mapping
4. **Create UI for permission management** - Allow administrators to grant/revoke permissions per role

Example migration code is provided in the documentation.

## Key Features

? **Clean Architecture** - Follows established layer separation  
? **Fine-Grained Control** - Workspace.Area.Action permission format  
? **Status-Based Access** - Different roles see different record statuses  
? **Multiple Authorization Strategies** - Attribute-based and imperative checking  
? **UI Integration** - Tag helpers for conditional rendering  
? **Extensible Design** - Easy to add new permissions and roles  
? **Documentation** - Comprehensive usage guide and examples  
? **Future-Ready** - Designed for database-driven permissions  
? **Backward Compatible** - Existing role-based policies maintained  

## Testing Recommendations

1. **Unit Tests** - Test PermissionService, RolePermissions, RoleStatusPermissions
2. **Integration Tests** - Test authorization handlers with mock users
3. **Controller Tests** - Test permission checking in action methods
4. **UI Tests** - Test tag helpers render correctly based on permissions
5. **End-to-End Tests** - Test complete permission workflows

## Next Steps

### Immediate
1. Update existing controllers to use permission-based authorization
2. Update existing views to use permission and record-status tag helpers
3. Add unit tests for new authorization components
4. Remove or update old authorization attributes where appropriate

### Future Enhancements
1. Move permissions to database (RoleClaims table)
2. Build UI for permission management
3. Add audit logging for permission checks
4. Implement permission caching for performance
5. Add permission groups/categories for easier management
6. Support hierarchical permissions (e.g., workspace.* grants all workspace permissions)

## Files Modified vs Created

### Created (11 files):
- Domain: Permission.cs
- Application: Permissions.cs, Roles.cs, RoleStatusPermissions.cs, RolePermissions.cs, IPermissionService.cs
- Infrastructure: PermissionRequirement.cs, PermissionAuthorizationHandler.cs, RecordStatusRequirement.cs, RecordStatusAuthorizationHandler.cs
- Web: PermissionTagHelper.cs, RecordStatusTagHelper.cs, PermissionExtensions.cs
- Docs: PERMISSIONS_SYSTEM.md, IMPLEMENTATION_SUMMARY.md

### Modified (2 files):
- AuthConfiguration.cs (added permission policies)
- ServicesConfiguration.cs (registered IPermissionService)

## Build Status

? **Build Successful** - All files compile without errors

## Conclusion

A production-ready, extensible permission system has been implemented that:
- Provides fine-grained access control
- Follows clean architecture principles
- Is easy to use for developers
- Supports future migration to database-driven permissions
- Maintains backward compatibility with existing authorization
- Includes comprehensive documentation

The system is ready for immediate use and can be extended as needed.
