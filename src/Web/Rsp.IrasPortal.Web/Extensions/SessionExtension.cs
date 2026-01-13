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
}