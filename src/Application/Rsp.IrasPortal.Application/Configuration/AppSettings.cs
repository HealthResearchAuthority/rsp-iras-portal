namespace Rsp.IrasPortal.Application.Configuration;

/// <summary>
/// Represents application settings.
/// </summary>
public struct AppSettings
{
    /// <summary>
    /// Gets or sets the URI of the categories microservice.
    /// </summary>
    public Uri? ApplicationsServiceUri { get; set; }

    /// <summary>
    /// Authentication settings for the application
    /// </summary>
    public AuthSettings AuthSettings { get; set; }
}