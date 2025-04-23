using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Rsp.IrasPortal.Web.TagHelpers;

/// <summary>
/// A TagHelper that automatically generates `aria-describedby` and `id` attributes for accessibility purposes,
/// based on bound model expressions.
/// </summary>
[HtmlTargetElement(Attributes = AriaDescribedByAttributeName)]
[HtmlTargetElement(Attributes = AriaIdAttributeName)]
public class AccessibilityTagHelper : TagHelper
{
    // Attribute names that trigger this tag helper
    private const string AriaDescribedByAttributeName = "aria-described-for";

    private const string AriaIdAttributeName = "aria-id-for";

    /// <summary>
    /// Model binding expression for generating the `aria-describedby` attribute.
    /// </summary>
    [HtmlAttributeName(AriaDescribedByAttributeName)]
    public ModelExpression DescribedFor { get; set; } = null!;

    /// <summary>
    /// Model binding expression for generating the `id` attribute.
    /// </summary>
    [HtmlAttributeName(AriaIdAttributeName)]
    public ModelExpression IdFor { get; set; } = null!;

    /// <summary>
    /// Processes the tag and injects the appropriate accessibility attributes.
    /// </summary>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Let the base tag helper logic run first
        await base.ProcessAsync(context, output);

        // Try to get a readable value from the bound model property
        var propertyName = (DescribedFor?.Model ?? IdFor?.Model) as string;

        // If there's no meaningful input, skip processing
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        // Build a safe string to be used as an ARIA attribute (e.g., question-text-hint)
        var ariaDescribedBy = BuildAriaDescribedBy(propertyName);

        // Set the aria-describedby attribute only if requested via DescribedFor
        if (!string.IsNullOrWhiteSpace(DescribedFor?.Name))
        {
            output.Attributes.SetAttribute("aria-describedby", ariaDescribedBy);
        }

        // Set the id attribute only if requested via IdFor
        if (!string.IsNullOrWhiteSpace(IdFor?.Name))
        {
            output.Attributes.SetAttribute("id", ariaDescribedBy);
        }
    }

    /// <summary>
    /// Sanitizes the provided string to ensure it is safe for use in HTML attributes.
    /// Replaces any character not allowed in HTML IDs with a dash and appends `-hint`.
    /// </summary>
    private static string BuildAriaDescribedBy(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Replace anything not alphanumeric or a dash with "-"
        var safeHint = SanitizeInputRegex().Replace(input, "-");

        // Clean up leading/trailing dashes, make lowercase, and append `-hint`
        return $"{safeHint.Trim('-').ToLowerInvariant()}-hint";
    }

    /// <summary>
    /// Regex pattern that removes all characters except letters, numbers, and dashes.
    /// Used to sanitize strings for HTML attribute safety.
    /// </summary>
    private static Regex SanitizeInputRegex()
    {
        return new Regex(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }
}