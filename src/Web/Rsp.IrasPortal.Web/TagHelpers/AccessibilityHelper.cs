using System.Text.RegularExpressions;

namespace Rsp.IrasPortal.Web.TagHelpers;

public static partial class AccessibilityHelper
{
    public static string BuildAriaDescribedBy(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var safeHint = SanitizeInputRegex().Replace(input, "-");
        return $"{safeHint.Trim('-').ToLowerInvariant()}-hint";
    }

    [GeneratedRegex(@"[^a-zA-Z0-9\-]")]
    private static partial Regex SanitizeInputRegex();
}