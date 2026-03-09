using Rsp.Portal.Application.Constants;

namespace Rsp.Portal.Web.Extensions;

/// <summary>
/// Provides extension methods for ISession to manage session values.
/// </summary>
public static class SessionExtension
{
    /// <summary>
    /// Removes all relevant session values used in the application.
    /// </summary>
    /// <param name="session">The session instance to operate on.</param>
    public static void RemoveAllSessionValues(this ISession session)
    {
        // Remove the ProjectRecord session value.
        session.Remove(SessionKeys.ProjectRecord);
    }

    public static void SetList(this ISession session, string key, List<string> value)
          => session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));

    public static List<string> GetList(this ISession session, string key)
        => session.GetString(key) is string json
           ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>()
           : new List<string>();
}