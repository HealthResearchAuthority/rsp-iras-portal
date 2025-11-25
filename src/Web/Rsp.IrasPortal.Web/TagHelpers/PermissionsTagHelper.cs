using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Extensions;

namespace Rsp.IrasPortal.Web.TagHelpers;

/// <summary>
/// A TagHelper to conditionally render content based on the current user's roles, permissions,
/// and optionally a record status value. Can be used either as a standalone element
/// (`<authorized-when>...`) or via attributes on other elements (e.g. `permission="..."`).
///
/// Behavior summary:
/// - If the user is not authenticated and `user-is-not-authenticated` is false (default), the content is suppressed.
/// - Either permission-based or role-based checks may be specified (not both).
/// - Multiple permissions/roles may be evaluated with `permission-mode` / `role-mode` using Any (OR) or All (AND).
/// - Optionally, the helper can also enforce access based on a model status value and a named status entity.
/// - Final visibility is the logical AND of the role/permission evaluation and the status evaluation.
/// </summary>
[HtmlTargetElement("authorized-when")] // standalone tag
[HtmlTargetElement(Attributes = UserNotAuthenticatedAttributeName)]
[HtmlTargetElement(Attributes = PermissionAttributeName)]
[HtmlTargetElement(Attributes = PermissionsAttributeName)]
[HtmlTargetElement(Attributes = PermissionModeAttributeName)]
[HtmlTargetElement(Attributes = RoleAttributeName)]
[HtmlTargetElement(Attributes = RolesAttributeName)]
[HtmlTargetElement(Attributes = RoleModeAttributeName)]
[HtmlTargetElement(Attributes = $"{StatusForAttribute},{StatusEntityAttributeName}")]
public class PermissionsTagHelper : TagHelper
{
    // -------------------------------
    // User not authenticated attributes
    // -------------------------------

    /// <summary>
    /// Attribute name used to indicate the element should be shown when the user is NOT authenticated.
    /// Defaults to false (do not show to anonymous users).
    /// </summary>
    private const string UserNotAuthenticatedAttributeName = "user-is-not-authenticated";

    // -------------------------------
    // Role attributes: Html attribute names for role based checks
    // -------------------------------

    private const string RoleAttributeName = "role";
    private const string RolesAttributeName = "roles";
    private const string RoleModeAttributeName = "role-mode";

    /// <summary>
    /// Mode to evaluate multiple roles:
    /// - Any: at least one role must match (OR)
    /// - All: all roles must match (AND)
    /// </summary>
    public enum RoleMode
    {
        Any,
        All
    }

    // -------------------------------
    // Permission attributes: Html attribute names for permission based checks
    // -------------------------------

    private const string PermissionAttributeName = "permission";
    private const string PermissionsAttributeName = "permissions";
    private const string PermissionModeAttributeName = "permission-mode";

    /// <summary>
    /// Mode to evaluate multiple permissions:
    /// - Any: at least one permission must be present (OR)
    /// - All: all permissions must be present (AND)
    /// </summary>
    public enum PermissionMode
    {
        Any,
        All
    }

    // -------------------------------
    // Status attributes: Html attribute names used to evaluate a model status value
    // -------------------------------

    private const string StatusForAttribute = "status-permission-for";

    // entity name to check status against e.g. projectrecord, modification, document
    private const string StatusEntityAttributeName = "status-entity";

    public override int Order => 0;

    /// <summary>
    /// When true, content will be shown if the user is NOT authenticated.
    /// Useful for showing UI to anonymous users only.
    /// </summary>
    [HtmlAttributeName(UserNotAuthenticatedAttributeName)]
    public bool ShowWhenUserIsNotAuthenticated { get; set; }

    // -------------------------------
    // Permission properties
    // -------------------------------

    /// <summary>
    /// Single permission to evaluate (maps to `permission="..."`).
    /// Mutually exclusive with `Permissions`.
    /// </summary>
    [HtmlAttributeName(PermissionAttributeName)]
    public string? Permission { get; set; }

    /// <summary>
    /// Collection of permissions to evaluate (maps to `permissions="..."`).
    /// Mutually exclusive with `Permission`.
    /// </summary>
    [HtmlAttributeName(PermissionsAttributeName)]
    public IEnumerable<string>? Permissions { get; set; }

    /// <summary>
    /// Determines whether multiple permissions are evaluated with Any (OR) or All (AND).
    /// Defaults to Any.
    /// </summary>
    [HtmlAttributeName(PermissionModeAttributeName)]
    public PermissionMode PermissionEvaluationLogic { get; set; } = PermissionMode.Any;

    // -------------------------------
    // Role properties
    // -------------------------------

    /// <summary>
    /// Single role to evaluate (maps to `role="..."`).
    /// Mutually exclusive with `Roles`.
    /// </summary>
    [HtmlAttributeName(RoleAttributeName)]
    public string? Role { get; set; }

    /// <summary>
    /// Collection of roles to evaluate (maps to `roles="..."`).
    /// Mutually exclusive with `Role`.
    /// </summary>
    [HtmlAttributeName(RolesAttributeName)]
    public IEnumerable<string>? RolesList { get; set; }

    /// <summary>
    /// Determines whether multiple roles are evaluated with Any (OR) or All (AND).
    /// Defaults to Any.
    /// </summary>
    [HtmlAttributeName(RoleModeAttributeName)]
    public RoleMode RoleEvaluationLogic { get; set; } = RoleMode.Any;

    /// <summary>
    /// ModelExpression that points to the status value to evaluate (e.g. `Model.Status`).
    /// Required together with `status-entity` to perform status-based access checks.
    /// </summary>
    [HtmlAttributeName(StatusForAttribute)]
    public ModelExpression? StatusFor { get; set; }

    /// <summary>
    /// Named status entity used in conjunction with `status-permission-for`. The pair is passed
    /// to `User.CanAccessRecordStatus(entity, status)` to decide status-based access.
    /// </summary>
    [HtmlAttributeName(StatusEntityAttributeName)]
    public string? StatusEntity { get; set; }

    /// <summary>
    /// Provides the current ViewContext (model and HTTP context). This property is injected
    /// by the runtime and must be marked HtmlAttributeNotBound so it is not treated as an attribute.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Convenience accessor for the current ClaimsPrincipal (the current user).
    /// Uses the HttpContext from the injected ViewContext.
    /// </summary>
    private ClaimsPrincipal User => ViewContext.HttpContext.User;

    /// <summary>
    /// Main processing entry point invoked by the Razor engine to transform the element.
    /// The method performs authentication check, validates attribute combinations, evaluates
    /// role/permission logic, applies optional status checks and suppresses output if access
    /// is not granted.
    /// </summary>
    /// <param name="context">TagHelperContext provided by the runtime.</param>
    /// <param name="output">TagHelperOutput used to modify the rendered output.</param>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // |Case   | ShowWhenUserIsNotAuthenticated | UserIsAuthenticated | Show Output |
        // |-------|--------------------------------|---------------------|-------------|
        // |Case 1 | true                           | false               | yes         |
        // |Case 2 | true                           | true                | no          |
        // |Case 3 | false                          | false               | no          |
        // |Case 4 | false                          | true                | yes         |

        // check if user is authenticated or not
        var isUserAuthenticated = User.Identity?.IsAuthenticated is true;

        // case 1: show to anonymous users only
        if (ShowWhenUserIsNotAuthenticated && !isUserAuthenticated)
        {
            if (output.TagName == "authorized-when")
            {
                output.TagName = null;
            }

            return;
        }

        // case 2: do not show to authenticated users
        if (ShowWhenUserIsNotAuthenticated)
        {
            output.SuppressOutput();
            return;
        }

        // case 3: if user is not authenticated, do not show
        if (!isUserAuthenticated)
        {
            output.SuppressOutput();
            return;
        }

        // case 4: Evaluate permissions and roles to determine if output should be shown.
        var showOutput = User.IsInRole(Roles.SystemAdministrator) || EvaluatePermissionsAndRoles();

        // If final decision is to not show output, suppress it.
        if (!showOutput)
        {
            output.SuppressOutput();
            return;
        }

        // When used as the standalone element <authorized-when>...</authorized-when>, unwrap the element
        // so that only the child content is rendered (remove the tag wrapper).
        if (output.TagName == "authorized-when")
        {
            output.TagName = null;
        }
    }

    public bool EvaluatePermissionsAndRoles()
    {
        // Determine whether permission or role attributes were provided
        var hasPermissionAttributes = HasPermissionAttributes();
        var hasRoleAttributes = HasRoleAttributes();

        // Enforce that callers must choose either permission-based OR role-based evaluation, not both.
        if (hasPermissionAttributes && hasRoleAttributes)
        {
            throw new InvalidOperationException("Cannot specify both permissions and roles at the same time.");
        }

        // Choose the correct evaluation strategy:
        // - If permissions specified => evaluate permissions
        // - If roles specified => evaluate roles
        // - If neither specified => default to granting access (no restrictions)
        bool permissionGranted = (hasPermissionAttributes, hasRoleAttributes) switch
        {
            (true, false) => EvaluatePermissions(),
            (false, true) => EvaluateRoles(),
            _ => true // No access attributes means access is granted
        };

        // Evaluate optional status-based access. This is combined with the role/permission result.
        bool userCanAccessStatus = UserCanAccessStatus();

        // Final visibility is the logical AND of permissionGranted and userCanAccessStatus.
        // If either check fails, suppress output.
        return permissionGranted && userCanAccessStatus;
    }

    // -------------------------------
    // Helpers
    // -------------------------------

    /// <summary>
    /// Validates and detects whether permission-related attributes have been provided.
    /// Ensures `permission` and `permissions` are not both specified.
    /// Returns true iff exactly one of them is provided.
    /// </summary>
    public bool HasPermissionAttributes()
    {
        // Disallow specifying both a single permission and a permissions collection.
        if (!string.IsNullOrWhiteSpace(Permission) && (Permissions?.Any() is true))
        {
            throw new InvalidOperationException("Cannot specify both permission and permissions");
        }

        // XOR: true if exactly one of the two is present
        return !string.IsNullOrWhiteSpace(Permission) ^ Permissions?.Any() is true;
    }

    /// <summary>
    /// Validates and detects whether role-related attributes have been provided.
    /// Ensures `role` and `roles` are not both specified.
    /// Returns true iff exactly one of them is provided.
    /// </summary>
    public bool HasRoleAttributes()
    {
        // Disallow specifying both single role and roles collection.
        if (!string.IsNullOrWhiteSpace(Role) && (RolesList?.Any() is true))
        {
            throw new InvalidOperationException("Cannot specify both role and roles");
        }

        // XOR: true if exactly one of the two is present
        return !string.IsNullOrWhiteSpace(Role) ^ RolesList?.Any() is true;
    }

    // -------------------------------
    // Permission logic
    // -------------------------------

    /// <summary>
    /// Evaluates permission-based access.
    /// - If a single `Permission` is provided, returns whether the user has that permission.
    /// - If a collection `Permissions` is provided, evaluates according to `PermissionEvaluationLogic`.
    /// Uses extension method `User.HasPermission(string)` to check individual permissions.
    /// </summary>
    public bool EvaluatePermissions()
    {
        if (Permission != null)
        {
            // Single permission check
            return User.HasPermission(Permission);
        }

        // Evaluate collection according to configured logic (Any/All).
        return PermissionEvaluationLogic switch
        {
            PermissionMode.Any => Permissions!.Any(User.HasPermission),
            PermissionMode.All => Permissions!.All(User.HasPermission),
            _ => false
        };
    }

    // -------------------------------
    // Role logic
    // -------------------------------

    /// <summary>
    /// Evaluates role-based access.
    /// - If a single `Role` is provided, returns whether the user is in that role.
    /// - If a collection `Roles` is provided, evaluates according to `RoleEvaluationLogic`.
    /// Uses built-in `User.IsInRole(string)` for checks.
    /// </summary>
    public bool EvaluateRoles()
    {
        if (Role != null)
        {
            // Single role check
            return User.IsInRole(Role);
        }

        // Evaluate collection according to configured logic (Any/All).
        return RoleEvaluationLogic switch
        {
            RoleMode.Any => RolesList!.Any(User.IsInRole),
            RoleMode.All => RolesList!.All(User.IsInRole),
            _ => false
        };
    }

    // -------------------------------
    // Status logic
    // -------------------------------

    /// <summary>
    /// If both `StatusFor` and `StatusEntity` are present, delegates to the extension method
    /// `User.CanAccessRecordStatus(entity, status)` to determine if the current user can access
    /// records with the given status for the named entity.
    /// If either value is missing/empty, the status check is considered passed (no restriction).
    /// </summary>
    public bool UserCanAccessStatus()
    {
        // If status information is not provided or empty, treat as no status restriction.
        if (string.IsNullOrWhiteSpace(StatusFor?.Model?.ToString()) || string.IsNullOrWhiteSpace(StatusEntity))
        {
            return true; // No status to evaluate means status check passes
        }

        // Delegate to extension method which encapsulates business rules for status access.
        return User.CanAccessRecordStatus(StatusEntity, StatusFor.Model.ToString()!);
    }
}