using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Rsp.Portal.Application.Configuration;

namespace Rsp.Portal.Configuration.AppConfiguration;

public static class AzureAppConfiguration
{
    /// <summary>
    /// Configures Azure App Configuration
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    public static IServiceCollection AddAzureAppConfiguration(this IServiceCollection services, ConfigurationManager configuration)
    {
        var azureAppSettingsSection = configuration.GetSection(nameof(AppSettings));
        var azureAppSettings = azureAppSettingsSection.Get<AppSettings>()!;

        // Load configuration from Azure App Configuration
        configuration.AddAzureAppConfiguration
        (
            options =>
            {
                var credentials =
                    new ManagedIdentityCredential(azureAppSettings.AzureAppConfiguration.IdentityClientID);
                options.Connect
                (
                    new Uri(azureAppSettings!.AzureAppConfiguration.Endpoint),
                    credentials
                )
                .Select(KeyFilter.Any) // select all the settings without any label
                .Select(KeyFilter.Any, AppSettings.ServiceLabel) // select all settings using the service name as label
                .ConfigureRefresh
                (
                    refreshOptions =>
                    {
                        // Sentinel is a special key, that is registered to monitor the change
                        // when this key is updated all of the keys will updated if refreshAll is true, after the cache is expired
                        // this won't restart the application, instead uses the middleware i.e. UseAzureAppConfiguration to refresh the keys
                        // IOptionsSnapshot<T> can be used to inject in the constructor, so that we get the latest values for T
                        // without this key, we would need to register all the keys we would like to monitor
                        refreshOptions
                            .Register("AppSettings:Sentinel", AppSettings.ServiceLabel, refreshAll: true)
                            .SetRefreshInterval(TimeSpan.FromSeconds(15));
                    }
                ).ConfigureKeyVault(options=>options.SetCredential(credentials));

                // enable feature flags
                options.UseFeatureFlags
                (
                    featureFlagOptions =>
                    {
                        featureFlagOptions
                            .Select(KeyFilter.Any) // select all flags without any label
                            .Select(KeyFilter.Any, AppSettings.ServiceLabel) // select all flags using the service name as label
                            .SetRefreshInterval(TimeSpan.FromSeconds(15));
                    }
                );
            }
        );

        services.AddAzureAppConfiguration();

        return services;
    }
}