using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rsp.IrasPortal.Infrastructure.HttpClients;

namespace Rsp.IrasPortal.Infrastructure.HealthChecks;

public class ServiceHealthCheck(IHealthCheckClient healthCheckClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var response = await healthCheckClient.GetServiceHealth();

        if (response.IsSuccessStatusCode)
        {
            return HealthCheckResult.Healthy("Categories Service is healthy.");
        }

        return HealthCheckResult.Unhealthy("Categories Service is unhealthy");
    }
}