# Example: Updating Existing Controllers to Use Permission System

## Before and After Examples

### Example 1: ModificationsController

#### Before (Using Role-Based Authorization)
```csharp
[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ModificationsController : Controller
{
    [HttpGet]
    public IActionResult CreateModification(string separator = "/")
    {
        // Create modification logic
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteModification(Guid projectModificationId)
    {
        // Delete modification logic
    }
}
```

#### After (Using Permission-Based Authorization)
```csharp
[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = Permissions.MyResearch.Modifications_Read)] // High-level workspace access
public class ModificationsController : Controller
{
    private readonly IPermissionService _permissionService;
    private readonly IProjectModificationsService _projectModificationsService;
    
    public ModificationsController(
        IPermissionService permissionService,
        IProjectModificationsService projectModificationsService,
        // ... other dependencies
    )
    {
        _permissionService = permissionService;
        _projectModificationsService = projectModificationsService;
    }
    
    [HttpGet]
    [Authorize(Policy = Permissions.MyResearch.Modifications_Create)]
    public IActionResult CreateModification(string separator = "/")
    {
        // Create modification logic
    }
    
    [HttpPost]
    [Authorize(Policy = Permissions.MyResearch.Modifications_Delete)]
    public async Task<IActionResult> DeleteModification(Guid projectModificationId)
    {
        // Get modification to check status
        var modificationResponse = await _projectModificationsService.GetModification(projectModificationId);
        
        if (!modificationResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(modificationResponse);
        }
        
        var modification = modificationResponse.Content;
        
        // Check if user can access modification based on its status
        if (!_permissionService.CanAccessRecordStatus(User, "modification", modification.Status))
        {
            return Forbid();
        }
        
        // Or use extension method
        var forbidResult = this.ForbidIfCannotAccessStatus(_permissionService, "modification", modification.Status);
        if (forbidResult != null)
        {
            return forbidResult;
        }
        
        // Proceed with deletion
        await _projectModificationsService.DeleteModification(projectModificationId);
        
        return RedirectToRoute("pov:postapproval", new { projectRecordId });
    }
}
```

### Example 2: SystemAdminController

#### Before
```csharp
[Route("[controller]/[action]", Name = "systemadmin:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
public class SystemAdminController : Controller
{
    [Route("/systemadmin", Name = "systemadmin:view")]
    public IActionResult Index()
    {
        return View(SystemAdminView);
    }
}
```

#### After
```csharp
[Route("[controller]/[action]", Name = "systemadmin:[action]")]
[Authorize(Policy = "IsSystemAdministrator")] // Keep for workspace-level access
public class SystemAdminController : Controller
{
    private readonly IPermissionService _permissionService;
    
    public SystemAdminController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }
    
    [Route("/systemadmin", Name = "systemadmin:view")]
    public IActionResult Index()
    {
        // Get user permissions to pass to view for conditional rendering
        ViewBag.UserPermissions = _permissionService.GetUserPermissions(User);
        
        return View(SystemAdminView);
    }
}
```

### Example 3: SponsorWorkspace AuthorisationsController

#### Before
```csharp
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
[Authorize(Policy = "IsSponsor")]
public class AuthorisationsController : ModificationsControllerBase
{
    [HttpGet]
    public async Task<IActionResult> CheckAndAuthorise(
        string projectRecordId, 
        string irasId, 
        string shortTitle,
        Guid projectModificationId, 
        Guid sponsorOrganisationUserId)
    {
        // Authorisation logic
    }
    
    [HttpPost]
    public async Task<IActionResult> CheckAndAuthorise(AuthoriseOutcomeViewModel model)
    {
        // Process authorisation
    }
}
```

#### After
```csharp
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
[Authorize(Policy = Permissions.Sponsor.SponsorReview_Read)]
public class AuthorisationsController : ModificationsControllerBase
{
    private readonly IPermissionService _permissionService;
    
    public AuthorisationsController(
        IPermissionService permissionService,
        // ... other dependencies
    ) : base(/* base dependencies */)
    {
        _permissionService = permissionService;
    }
    
    [HttpGet]
    [Authorize(Policy = Permissions.Sponsor.SponsorReview_Review)]
    public async Task<IActionResult> CheckAndAuthorise(
        string projectRecordId, 
        string irasId, 
        string shortTitle,
        Guid projectModificationId, 
        Guid sponsorOrganisationUserId)
    {
        // Get modification to check status
        var modificationResponse = await projectModificationsService.GetModification(projectModificationId);
        
        if (!modificationResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(modificationResponse);
        }
        
        var modification = modificationResponse.Content;
        
        // Check if sponsor can access modification in current status
        if (!_permissionService.CanAccessRecordStatus(User, "modification", modification.Status))
        {
            return Forbid();
        }
        
        // Authorisation logic
    }
    
    [HttpPost]
    [Authorize(Policy = Permissions.Sponsor.SponsorReview_Authorise)]
    public async Task<IActionResult> CheckAndAuthorise(AuthoriseOutcomeViewModel model)
    {
        // Additional check: can only authorise if modification is "With sponsor"
        if (!_permissionService.CanAccessRecordStatus(User, "modification", ModificationStatus.WithSponsor))
        {
            return Forbid();
        }
        
        // Process authorisation
    }
}
```

## Updating Views

### Before
```razor
@* Check role directly *@
<authorized auth-params="@new(Roles:"applicant")">
    <a asp-action="CreateProject" class="govuk-button">Add project</a>
</authorized>

@* No status-based rendering *@
<a asp-action="DeleteModification" asp-route-id="@Model.Id" class="govuk-link">Delete</a>
```

### After
```razor
@inject IPermissionService PermissionService
@using Rsp.IrasPortal.Application.Constants

@* Use permission tag helper *@
<div permission="@Permissions.MyResearch.ProjectRecord_Create">
    <a asp-action="CreateProject" class="govuk-button">Add project</a>
</div>

@* Use status-based rendering *@
<div record-status="modification:@Model.Status">
    <div permission="@Permissions.MyResearch.Modifications_Delete">
        <a asp-action="DeleteModification" asp-route-id="@Model.Id" class="govuk-link">Delete</a>
    </div>
</div>

@* Conditional logic based on permissions *@
@{
    var canUpdate = PermissionService.HasPermission(User, Permissions.MyResearch.Modifications_Update);
    var canDelete = PermissionService.HasPermission(User, Permissions.MyResearch.Modifications_Delete);
    var canAccessStatus = PermissionService.CanAccessRecordStatus(User, "modification", Model.Status);
}

@if (canAccessStatus)
{
    <div class="action-buttons">
        @if (canUpdate)
        {
            <a asp-action="EditModification" asp-route-id="@Model.Id" class="govuk-button">Edit</a>
        }
        
        @if (canDelete && Model.Status == ModificationStatus.InDraft)
        {
            <a asp-action="DeleteModification" asp-route-id="@Model.Id" class="govuk-button govuk-button--warning">Delete</a>
        }
    </div>
}
else
{
    <p class="govuk-body">You do not have permission to access this record.</p>
}
```

## Updating Existing Views with Home Dashboard

### Before (ResearchAccount/Index.cshtml)
```razor
@{
    var cards = new[]
    {
        new
        {
            Roles = "applicant",
            Title = myResearchLabel?.Value,
            Desc = myResearchDescription?.Value,
            Link = "app:welcome"
        },
        new
        {
            Roles = "sponsor",
            Title = sponsorsLabel?.Value,
            Desc = sponsorsDescription?.Value,
            Link = "sws:sponsorworkspace"
        },
        new
        {
            Roles = "system_administrator",
            Title = systemAdministrationLabel?.Value,
            Desc = systemAdministrationDescription?.Value,
            Link = "systemadmin:view"
        },
    };

    var authorizedChunks = cards
        .Where(item =>
        {
            var roles = item.Roles?.Split(',') ?? [];
            return roles.Any(role => User.IsInRole(role));
        })
        // ... grouping logic
}

@foreach (var row in authorizedChunks)
{
    <div class="govuk-grid-row">
        @foreach (var item in row)
        {
            <authorized auth-params="@new(Roles: item.Roles)">
                <div class="govuk-grid-column-one-third">
                    <partial name="_ActionItem" model="@(item.Title, item.Desc, item.Link, new Dictionary<string, string>())" />
                </div>
            </authorized>
        }
    </div>
}
```

### After (with Permission-Based Rendering)
```razor
@inject IPermissionService PermissionService
@using Rsp.IrasPortal.Application.Constants

@{
    var cards = new[]
    {
        new
        {
            Permission = Permissions.MyResearch.ProjectRecord_Read,
            Title = myResearchLabel?.Value,
            Desc = myResearchDescription?.Value,
            Link = "app:welcome"
        },
        new
        {
            Permission = Permissions.Sponsor.SponsorReview_Read,
            Title = sponsorsLabel?.Value,
            Desc = sponsorsDescription?.Value,
            Link = "sws:sponsorworkspace"
        },
        new
        {
            Permission = Permissions.SystemAdministration.ManageUsers_Read,
            Title = systemAdministrationLabel?.Value,
            Desc = systemAdministrationDescription?.Value,
            Link = "systemadmin:view"
        },
        new
        {
            Permission = Permissions.Approvals.SearchSubmittedRecords_Read,
            Title = approvalsLabel?.Value,
            Desc = approvalsDescription?.Value,
            Link = "approvalsmenu:welcome"
        },
    };

    var authorizedChunks = cards
        .Where(item => PermissionService.HasPermission(User, item.Permission))
        .Select((card, index) => new { card, index })
        .GroupBy(x => x.index / 3)
        .Select(g => g.Select(x => x.card).ToList())
        .ToList();
}

@foreach (var row in authorizedChunks)
{
    <div class="govuk-grid-row">
        @foreach (var item in row)
        {
            <div permission="@item.Permission">
                <div class="govuk-grid-column-one-third">
                    <partial name="_ActionItem" model="@(item.Title, item.Desc, item.Link, new Dictionary<string, string>())" />
                </div>
            </div>
        }
    </div>
}
```

## Migration Checklist

### For Each Controller:
- [ ] Add `IPermissionService` dependency injection
- [ ] Replace `[Authorize(Policy = "IsRoleName")]` with specific permission policies
- [ ] Add status-based checks for actions that modify data
- [ ] Use `ForbidIfNoPermission` or `ForbidIfCannotAccessStatus` extension methods
- [ ] Pass user permissions to views via ViewBag if needed

### For Each View:
- [ ] Add `@inject IPermissionService PermissionService`
- [ ] Replace role checks with permission tag helpers
- [ ] Add status-based rendering where applicable
- [ ] Use `PermissionService.HasPermission()` for complex conditional logic
- [ ] Test that UI elements show/hide correctly for different roles

### For Each Feature:
- [ ] Identify all permissions needed (Create, Read, Update, Delete, etc.)
- [ ] Update `RolePermissions` mappings if new permissions added
- [ ] Update `RoleStatusPermissions` if new statuses or entity types added
- [ ] Add permission policies in `AuthConfiguration` if using attribute-based authorization
- [ ] Write unit tests for permission logic
- [ ] Document any new permissions or patterns

## Common Patterns

### Pattern 1: CRUD Operations with Status Checks
```csharp
// READ - Check both permission and status
var modification = await GetModification(id);
if (!_permissionService.CanAccessRecordStatus(User, "modification", modification.Status))
{
    return Forbid();
}

// UPDATE - Check permission, status, and specific status requirement
var forbidResult = this.ForbidIfNoPermission(_permissionService, Permissions.MyResearch.Modifications_Update);
if (forbidResult != null) return forbidResult;

if (!_permissionService.CanAccessRecordStatus(User, "modification", modification.Status))
{
    return Forbid();
}

// Only allow update if in draft
if (modification.Status != ModificationStatus.InDraft)
{
    return BadRequest("Can only update draft modifications");
}

// DELETE - Similar checks as update
```

### Pattern 2: Listing Records with Status Filtering
```csharp
public async Task<IActionResult> ListModifications()
{
    // Get allowed statuses for current user
    var allowedStatuses = _permissionService.GetAllowedStatuses(User, "modification");
    
    // Filter modifications by allowed statuses
    var allModifications = await _modificationsService.GetModifications();
    var accessibleModifications = allModifications
        .Where(m => allowedStatuses.Contains(m.Status, StringComparer.OrdinalIgnoreCase))
        .ToList();
    
    return View(accessibleModifications);
}
```

### Pattern 3: Dynamic Button Visibility in Views
```razor
<div class="action-buttons">
    <div permission="@Permissions.MyResearch.Modifications_Update" record-status="modification:@Model.Status">
        <a asp-action="Edit" asp-route-id="@Model.Id" class="govuk-button">Edit</a>
    </div>
    
    <div permission="@Permissions.MyResearch.Modifications_Delete">
        @if (Model.Status == ModificationStatus.InDraft)
        {
            <a asp-action="Delete" asp-route-id="@Model.Id" class="govuk-button govuk-button--warning">Delete</a>
        }
    </div>
</div>
```

## Testing Your Changes

### 1. Test with Different Roles
```csharp
// Create users with different roles
var applicant = CreateUserWithRole(Roles.Applicant);
var sponsor = CreateUserWithRole(Roles.Sponsor);
var admin = CreateUserWithRole(Roles.SystemAdministrator);

// Test permissions
Assert.True(applicant.HasPermission(Permissions.MyResearch.ProjectRecord_Create));
Assert.False(sponsor.HasPermission(Permissions.MyResearch.ProjectRecord_Create));
Assert.True(admin.HasPermission(Permissions.SystemAdministration.ManageUsers_Delete));
```

### 2. Test Status-Based Access
```csharp
// Test applicant can see draft modifications
var draftModification = new Modification { Status = ModificationStatus.InDraft };
Assert.True(_permissionService.CanAccessRecordStatus(applicant, "modification", draftModification.Status));

// Test sponsor cannot see draft modifications
Assert.False(_permissionService.CanAccessRecordStatus(sponsor, "modification", draftModification.Status));

// Test sponsor can see "With sponsor" modifications
var withSponsorModification = new Modification { Status = ModificationStatus.WithSponsor };
Assert.True(_permissionService.CanAccessRecordStatus(sponsor, "modification", withSponsorModification.Status));
```

### 3. Test UI Rendering
- Log in as different users
- Verify correct buttons/links are visible
- Verify correct workspaces are accessible
- Verify status-based content shows/hides correctly

## Conclusion

The permission system provides a flexible, maintainable way to control access at both the action and record levels. By following these patterns and examples, you can systematically update existing controllers and views to use the new permission system while maintaining backward compatibility.
