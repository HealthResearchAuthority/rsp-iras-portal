namespace Rsp.IrasPortal.Application.Configuration;

/// <summary>
/// Represents application settings.
/// </summary>
public struct AppSettings
{
    /// <summary>
    /// Gets or sets the URI of the categories microservice.
    /// </summary>
    public Uri? CategoriesServiceUri { get; set; }

    /// <summary>
    /// Authority URL configured for OpenId
    /// </summary>
    public string Authority { get; set; }

    /// <summary>
    /// ClientId for OpenId Auth Configuration
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// ClientSecret for OpenId Auth Configuration
    /// </summary>
    public string ClientSecret { get; set; }
}