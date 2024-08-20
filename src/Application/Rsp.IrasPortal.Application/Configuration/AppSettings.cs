namespace Rsp.IrasPortal.Application.Configuration;

/// <summary>
/// Represents application settings.
/// </summary>
public class AppSettings
{
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
    /// Authentication settings for the application
    /// </summary>
    public AuthSettings AuthSettings { get; set; } = null!;

    public AzureAppConfigurations AzureAppConfiguration { get; set; } = null!;
}