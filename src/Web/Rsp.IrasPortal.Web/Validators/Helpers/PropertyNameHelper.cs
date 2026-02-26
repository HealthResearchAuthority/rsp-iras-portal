using System.Text.RegularExpressions;

namespace Rsp.Portal.Web.Validators.Helpers;

public static class PropertyNameHelper
{
    // Replace Questions[anyNumber] with the correct index
    public static string AdjustPropertyName(string propertyName, int index)
    {
        if (string.IsNullOrEmpty(propertyName) || !propertyName.StartsWith("Questions["))
            return propertyName;

        // Pattern: match "Questions[<any number>]."
        var pattern = @"Questions\[\d+\]\.";

        // Replacement: inject the given index
        var replacement = $"Questions[{index}].";

        // Always pass a timeout (e.g., 100 ms)
        return Regex.Replace(propertyName, pattern, replacement, RegexOptions.None, TimeSpan.FromMilliseconds(100));
    }
}