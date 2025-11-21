using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.TagHelpers;

/// <summary>
/// Tag helper that conditionally renders content based on user permissions
/// Usage: &lt;div permission="myresearch.projectrecord.create"&gt;...&lt;/div&gt;
/// </summary>
[HtmlTargetElement("*", Attributes = "permission")]
public class PermissionTagHelper(IPermissionService permissionService) : TagHelper
{
    /// <summary>
    /// The required permission (e.g., "myresearch.projectrecord.create")
    /// </summary>
    [HtmlAttributeName("permission")]
    public string? Permission { get; set; }

    /// <summary>
    /// Provides the current ViewContext, which contains information about the view and model state.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Permission))
        {
            return;
        }

        var user = ViewContext.HttpContext?.User;

        if (user == null)
        {
            output.SuppressOutput();
            return;
        }

        var hasPermission = permissionService.HasPermission(user, Permission);

        // Normal logic: show if has permission, hide if doesn't
        if (!hasPermission)
        {
            output.SuppressOutput();
        }
        else
        {
            // this is to avoid rendering an empty div if no other attributes are present
            // this is a scenario where the tag helper is used on a div without any other attributes
            // to group content that requires permission
            if (output.TagName == "div" && output.Attributes.Count == 0)
            {
                output.TagName = null; // Render only content if no attributes
            }
        }

        // Remove the custom attribute from output
        output.Attributes.RemoveAll("permission");
    }
}