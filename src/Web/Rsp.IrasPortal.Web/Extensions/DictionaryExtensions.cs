namespace Rsp.IrasPortal.Web.Extensions;

public static class DictionaryExtensions
{
    /// <summary>
    /// Gets the value for the given key, or returns a default value if not found.
    /// </summary>
    public static string GetValueOrDefault(this IDictionary<string, string?> dict, string key, string defaultValue = "")
    {
        if (dict == null)
            throw new ArgumentNullException(nameof(dict));

        if (key == null)
            throw new ArgumentNullException(nameof(key));

        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }
}