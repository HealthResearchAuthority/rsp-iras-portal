using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Rsp.IrasPortal.Web.TagHelpers;

/// <summary>
/// A TagHelper that appends error-specific CSS classes to form elements based on the model state.
/// </summary>
[HtmlTargetElement("*", Attributes = ForAttributeName)]
[HtmlTargetElement("*", Attributes = ForPropertyName)]
public class ErrorClassTagHelper : TagHelper
{
    // Attribute name for the property if model wasn't specified.
    private const string ForPropertyName = "error-class-property";

    // Attribute name for binding the model expression.
    private const string ForAttributeName = "error-class-for";

    // Attribute name for specifying the error class for non-input elements.
    private const string ErrorClassAttributeName = "error-class";

    // Attribute name for specifying the error class for input elements.
    private const string InputErrorClassAttributeName = "input-error-class";

    /// <summary>
    /// Provides the current ViewContext, which contains information about the view and model state.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// The model expression
    /// </summary>
    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression? For { get; set; }

    /// <summary>
    /// The model prperty
    /// </summary>
    [HtmlAttributeName(ForPropertyName)]
    public string? Property { get; set; }

    /// <summary>
    /// The CSS class to apply when the associated model property has validation errors (non-input elements).
    /// </summary>
    [HtmlAttributeName(ErrorClassAttributeName)]
    public string ErrorClass { get; set; } = "govuk-form-group--error";

    /// <summary>
    /// The CSS class to apply when the associated model property has validation errors (input elements).
    /// </summary>
    [HtmlAttributeName(InputErrorClassAttributeName)]
    public string InputErrorClass { get; set; } = "govuk-input--error";

    /// <summary>
    /// Processes the TagHelper to append the appropriate error class if the model state contains errors for the specified property.
    /// </summary>
    /// <param name="context">The context of the TagHelper.</param>
    /// <param name="output">The output of the TagHelper.</param>
    /// <returns>A completed Task.</returns>
    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Call the base implementation (not strictly necessary here, but included for completeness).
        base.ProcessAsync(context, output);

        // when using this tag helper on any input element, it will utilise
        // For.Name. If we are passing in the name to the template, we need to use the actual value
        // hence For.Model is also checked.
        var key = For?.Name ?? Property;

        var modelStateEntry = ViewContext.ViewData.ModelState[key!];

        // Check if the model state contains errors for the specified property.
        if (modelStateEntry?.Errors.Count is null or 0)
        {
            return Task.CompletedTask;
        }

        // Determine the appropriate CSS class to apply based on the tag name.
        var @class = context?.TagName == "input" ?
                                InputErrorClass :
                                ErrorClass;

        // Add the determined CSS class to the element.
        output.AddClass(@class, HtmlEncoder.Default);

        return Task.CompletedTask;
    }
}