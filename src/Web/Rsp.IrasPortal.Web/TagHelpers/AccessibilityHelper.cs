using System.Text.RegularExpressions;

namespace Rsp.IrasPortal.Web.TagHelpers;

public static class AccessibilityHelper
{
    public static string BuildAriaDescribedBy(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var safeHint = Regex.Replace(input, @"[^a-zA-Z0-9\-]", "-");
        return $"{safeHint.Trim('-').ToLowerInvariant()}-hint";
    }
}