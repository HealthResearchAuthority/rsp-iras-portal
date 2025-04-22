using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.RegularExpressions;

namespace Rsp.IrasPortal.Web.TagHelpers;

[HtmlTargetElement(Attributes = AriaDescribedByAttributeName)]
[HtmlTargetElement(Attributes = AriaIdAttributeName)]
public class BuildAriaTagHelper : TagHelper
{
    private const string AriaDescribedByAttributeName = "aria-described-for";
    private const string AriaIdAttributeName = "aria-id-for";

    [HtmlAttributeName(AriaDescribedByAttributeName)]
    public ModelExpression DescribedFor { get; set; } = null!;

    [HtmlAttributeName(AriaIdAttributeName)]
    public ModelExpression IdFor { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);

        var propertyName = (DescribedFor?.Model ?? IdFor?.Model) as string;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        var ariaDescribedBy = AccessibilityHelper.BuildAriaDescribedBy(propertyName);
        if (!string.IsNullOrWhiteSpace(DescribedFor?.Name))
        {
            output.Attributes.SetAttribute("aria-describedby", ariaDescribedBy);
        }

        if (!string.IsNullOrWhiteSpace(IdFor?.Name))
        {
            output.Attributes.SetAttribute("id", ariaDescribedBy);
        }
    }
}

public static partial class AccessibilityHelper
{
    public static string BuildAriaDescribedBy(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        var safeHint = SanitizeInputRegex().Replace(input, "-");
        return $"{safeHint.Trim('-').ToLowerInvariant()}-hint";
    }

    [GeneratedRegex(@"[^a-zA-Z0-9\-]")]
    private static partial Regex SanitizeInputRegex();
}