namespace Rsp.IrasPortal.Application.Configuration;

public class AuthSettings
{
    /// <summary>
    /// The value for token issuer / authority
    /// </summary>
    public string Authority { get; set; } = null!;

    /// <summary>
    /// The client identifier.
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// The client secret.
    /// </summary>
    public string ClientSecret { get; set; } = null!;
}