namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// Custom claim types used in the application.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// The user's status key for storing claim value (e.g., active, disabled).
    /// </summary>
    public const string UserStatus = "user_status";

    /// <summary>
    /// The user's ID key for storing the userId claim value.
    /// </summary>
    public const string UserId = "userId";

    /// <summary>
    /// The permissions key for storing the permissions claim value.
    /// </summary>
    public const string Permissions = "permissions";
}