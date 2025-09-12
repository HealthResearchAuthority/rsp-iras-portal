using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Configuration.Health;

/// <summary>
/// Health Checks Configuration
/// </summary>
public static class HealthChecksConfiguration
{
    /// <summary>
    /// Adds the Health Checks to the service
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="appSettings">Application Settings</param>
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, AppSettings appSettings)
    {
        static HeaderPropagationMessageHandler AddHeaderPropagation()
        {
            // the following setup was done to propgate the
            // correlationId header to the the health check call as well
            var options = new HeaderPropagationMessageHandlerOptions();

            options.Headers.Add(RequestHeadersKeys.CorrelationId);

            return new HeaderPropagationMessageHandler(options, new())
            {
                InnerHandler = new HttpClientHandler()
            };
        }

        var applicationServiceUri = new Uri(appSettings.ApplicationsServiceUri!, "/probes/liveness");
        var userServiceUri = new Uri(appSettings.UsersServiceUri!, "/probes/liveness");

        services
           .AddHealthChecks()
           .AddUrlGroup(applicationServiceUri, "Iras Applications API", HealthStatus.Unhealthy, configurePrimaryHttpMessageHandler: _ => AddHeaderPropagation())
           .AddUrlGroup(userServiceUri, "User Service API", HealthStatus.Unhealthy, configurePrimaryHttpMessageHandler: _ => AddHeaderPropagation());

        return services;
    }
}