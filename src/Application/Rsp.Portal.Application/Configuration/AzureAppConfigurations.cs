namespace Rsp.Portal.Application.Configuration;

public class AzureAppConfigurations
{
    /// <summary>
    /// Azure App Configuration Endpoint
    /// </summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>
    /// The Managed Identity client identifier
    /// </summary>
    public string IdentityClientID { get; set; } = null!;
}