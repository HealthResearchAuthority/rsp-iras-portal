using System.Diagnostics.CodeAnalysis;

namespace Rsp.IrasPortal.Application.Configuration;

[ExcludeFromCodeCoverage]
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