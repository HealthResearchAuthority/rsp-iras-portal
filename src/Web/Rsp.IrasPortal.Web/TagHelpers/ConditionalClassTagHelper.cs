using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Rsp.IrasPortal.Web.TagHelpers;

/// <summary>
/// A TagHelper that appends error-specific CSS classes to form elements based on the model state.
/// </summary>
[HtmlTargetElement("*", Attributes = ConditionalAttributeName)]
public class CssClassTagHelper : TagHelper
{
    // Attribute name to mark the element as conditional.
    private const string ConditionalAttributeName = "conditional";

    // Attribute name for specifying the conditional class for conditional elements.
    private const string ConditionalClassAttributeName = "conditional-class";

    /// <summary>
    /// If true, the conditional CSS class will be added to the element.
    /// </summary>
    [HtmlAttributeName(ConditionalAttributeName)]
    public bool Conditional { get; set; }

    /// <summary>
    /// The CSS class to apply when the element is marked as Conditional.
    /// </summary>
    [HtmlAttributeName(ConditionalClassAttributeName)]
    public string ConditionalClass { get; set; } = "conditional";

    /// <summary>
    /// Processes the TagHelper to append the appropriate conditional class if the element is marked as conditional.
    /// </summary>
    /// <param name="context">The context of the TagHelper.</param>
    /// <param name="output">The output of the TagHelper.</param>
    /// <returns>A completed Task.</returns>
    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Call the base implementation (not strictly necessary here, but included for completeness).
        base.ProcessAsync(context, output);

        // If the element is conditional, add the "conditional" class.
        if (Conditional)
        {
            output.AddClass(ConditionalClass, HtmlEncoder.Default);
        }

        return Task.CompletedTask;
    }
}