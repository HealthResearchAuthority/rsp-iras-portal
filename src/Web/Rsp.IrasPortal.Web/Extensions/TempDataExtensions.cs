using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Rsp.IrasPortal.Web.Extensions;

/// <summary>
/// Allows adding complex object to TempData by serializing it.
/// </summary>
public static class TempDataExtensions
{
    /// <summary>
    /// Tries to add specified <paramref name="key"/> and <paramref name="value"/> to the dictionary
    /// </summary>
    /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
    /// <param name="dictionary"><see cref="ITempDataDictionary"/></param>
    /// <param name="key">The key of the <paramref name="value"/> to add.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="serialize">if true, the <paramref name="value"/> will be serialized</param>
    /// <returns>
    /// <see cref="true"/> when <paramref name="key"/> and <paramref name="value"/> are successfully added to the <paramref name="dictionary"/>;
    /// <see cref="false"/> when <paramref name="dictionary"/> already contains the <paramref name="key"/>, in which case nothing gets added.
    /// </returns>
    /// <exception cref="ArgumentNullException" />
    public static bool TryAdd<T>(this ITempDataDictionary dictionary, string key, T value, bool serialize) where T : class
    {
        return serialize switch
        {
            true => dictionary.TryAdd(key, JsonSerializer.Serialize(value)),
            _ => dictionary.TryAdd(key, value)
        };
    }

    /// <summary>
    ///  Gets the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">Type of <paramref name="value"/> to get</typeparam>
    /// <param name="dictionary"><see cref="ITempDataDictionary"/></param>
    /// <param name="key">The key whose <paramref name="value"/> to get.</param>
    /// <param name="value">
    /// When this method returns, the value associated with the specified key, if the
    /// key is found; otherwise, the default value for the type of the value parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <param name="deserialize">if true, the <typeparamref name="T"/> will be deserialized</param>
    /// <returns>
    /// true if the object that implements <see cref="IDictionary{TKey, TValue}" /> contains
    /// an element with the specified key; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">key is null.</exception>
    public static bool TryGetValue<T>(this ITempDataDictionary dictionary, string key, out T? value, bool deserialize) where T : class
    {
        value = default;

        if (!dictionary.TryGetValue(key, out var objValue) || objValue is null)
        {
            return false;
        }

        value = deserialize switch
        {
            true => JsonSerializer.Deserialize<T>((string)objValue),
            _ => (T)objValue
        };

        return true;
    }
}