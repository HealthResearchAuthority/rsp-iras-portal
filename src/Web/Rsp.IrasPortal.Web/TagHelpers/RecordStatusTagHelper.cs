using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.TagHelpers;

/// <summary>
/// Tag helper that conditionally renders content based on record status access
/// Usage: &lt;div record-status="modification:In draft"&gt;...&lt;/div&gt;
/// </summary>
[HtmlTargetElement(Attributes = "record-status")]
public class RecordStatusTagHelper
(
    IPermissionService permissionService,
    IHttpContextAccessor httpContextAccessor
) : TagHelper
{
    /// <summary>
    /// The entity type and status in format "entityType:status"
    /// (e.g., "modification:In draft", "projectrecord:Active")
    /// </summary>
    [HtmlAttributeName("record-status")]
    public string? RecordStatus { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(RecordStatus))
        {
            return;
        }

        var user = httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            output.SuppressOutput();
            return;
        }

        // Parse the record-status attribute
        var parts = RecordStatus.Split(':', 2);
        if (parts.Length != 2)
        {
            output.SuppressOutput();
            return;
        }

        var entityType = parts[0].Trim();
        var status = parts[1].Trim();

        var canAccess = permissionService.CanAccessRecordStatus(user, entityType, status);

        if (!canAccess)
        {
            output.SuppressOutput();
        }

        // Remove the custom attribute from output
        output.Attributes.RemoveAll("record-status");
    }
}