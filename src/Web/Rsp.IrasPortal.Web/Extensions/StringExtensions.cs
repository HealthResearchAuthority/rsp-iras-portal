namespace Rsp.IrasPortal.Web.Extensions;

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
}