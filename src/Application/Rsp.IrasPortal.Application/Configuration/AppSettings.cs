namespace Rsp.IrasPortal.Application.Configuration;

/// <summary>
/// Represents application settings.
/// </summary>
public class AppSettings
{
    public string AllowedHosts { get; set; } = null!;

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
    /// Gets or sets the URI of the RTS microservice
    /// </summary>
    public Uri RtsServiceUri { get; set; } = null!;

    /// <summary>
    /// Gets or sets the base URI of the CMS service.
    /// </summary>
    public Uri CmsUri { get; set; } = null!;

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

    /// <summary>
    /// Number of minutes before the session expires to show a warning to the user.
    /// </summary>
    public uint WarningBeforeSeconds { get; set; }

    /// <summary>
    /// Microsoft clarity project id
    /// </summary>
    public string? ClarityProjectId { get; set; }

    /// <summary>
    /// Number of seconds the general page CMS content is cached
    /// </summary>
    public int? GeneralContentCacheDurationSeconds { get; set; }

    /// <summary>
    /// Number of seconds the global page CMS content is cached (eg. footer)
    /// </summary>
    public int? GlobalContentCacheDurationSeconds { get; set; }
}