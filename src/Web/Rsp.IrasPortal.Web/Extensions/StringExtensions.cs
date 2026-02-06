using System.Text.RegularExpressions;

namespace Rsp.Portal.Web.Extensions;

public static class StringExtensions
{
    public static string ToSentenceCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        input = input.Trim();

        if (input.Length == 1)
            return input.ToUpper();

        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }

    public static string GetLabelText(this string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return label;

        var acronymsPattern = @"\b(nhs|hsc|nct|isrctn)\b";
        var phrasesPattern = @"\b(chief investigator|principal investigator)\b";

        return Regex.Replace(
            label.ToLowerInvariant(),
            $"{acronymsPattern}|{phrasesPattern}",
            m =>
            {
                var value = m.Value;
                return Regex.IsMatch(value, acronymsPattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100))
                    ? value.ToUpperInvariant()
                    : string.Join(" ", value.Split(' ').Select(w => char.ToUpper(w[0]) + w.Substring(1)));
            },
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100)
        );
    }
}