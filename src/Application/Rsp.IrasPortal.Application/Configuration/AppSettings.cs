namespace Rsp.IrasPortal.Application.Configuration;

/// <summary>
/// Represents application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Label to use when reading App Configuration from AzureAppConfiguration
    /// </summary>
    public const string ServiceLabel = "portal";

    /// <summary>
    /// Gets or sets the URI of the categories microservice.
    /// </summary>
    public Uri ApplicationsServiceUri { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URI of the users management microservice
    /// </summary>
    public Uri UsersServiceUri { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URI of the questions set microservice
    /// </summary>
    public Uri QuestionSetServiceUri { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URI of the RTS microservice
    /// </summary>
    public Uri RtsServiceUri { get; set; } = null!;

    /// <summary>
    /// Authentication settings for the application
    /// </summary>
    public AuthSettings AuthSettings { get; set; } = null!;

    /// <summary>
    /// Azure App Configuration settings
    /// </summary>
    public AzureAppConfigurations AzureAppConfiguration { get; set; } = null!;

    /// <summary>
    /// OneLogin configuration
    /// </summary>
    public OneLoginConfiguration OneLogin { get; set; } = null!;

    /// <summary>
    /// Timeout for the session in seconds.
    /// </summary>
    public uint SessionTimeout { get; set; }
}