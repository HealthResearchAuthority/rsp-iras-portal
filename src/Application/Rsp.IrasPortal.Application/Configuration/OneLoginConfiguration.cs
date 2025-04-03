namespace Rsp.IrasPortal.Application.Configuration;

public class OneLoginConfiguration
{
    /// <summary>
    /// The authority to use for the JWT token
    /// </summary>
    public string Authority { get; set; } = null!;

    /// <summary>
    /// Private key for signing the JWT token
    /// </summary>
    public string PrivateKeyPem { get; set; } = null!;

    /// <summary>
    /// The client identifier.
    /// </summary>
    public string ClientId { get; set; } = null!;
}