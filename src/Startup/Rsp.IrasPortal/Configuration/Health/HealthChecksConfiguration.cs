using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Configuration.Health;

/// <summary>
/// Health Checks Configuration
/// </summary>
[ExcludeFromCodeCoverage]
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

        options.Headers.Add(CustomRequestHeaders.CorrelationId);

        return new HeaderPropagationMessageHandler(options, new())
        {
            InnerHandler = new HttpClientHandler()
        };
    }

    var applicationserviceuri = new Uri(appSettings.ApplicationsServiceUri!, "/probes/liveness");
    var userserviceuri = new Uri(appSettings.UsersServiceUri!, "/probes/liveness");

    services
       .AddHealthChecks()
       .AddUrlGroup(applicationserviceuri, "Iras Applications API", HealthStatus.Unhealthy, configurePrimaryHttpMessageHandler: _ => AddHeaderPropagation())
       .AddUrlGroup(userserviceuri, "User Service API", HealthStatus.Unhealthy, configurePrimaryHttpMessageHandler: _ => AddHeaderPropagation());

        services
           .AddHealthChecksUI
           (
                opt =>
                {
                    opt.SetEvaluationTimeInSeconds(300); //time in seconds between check
                    opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks
                    opt.SetApiMaxActiveRequests(1); //api requests concurrency
                    opt.AddHealthCheckEndpoint("Health Status", "/portal-health"); //map health check api
                }
            ).AddInMemoryStorage();

        return services;
    }
}