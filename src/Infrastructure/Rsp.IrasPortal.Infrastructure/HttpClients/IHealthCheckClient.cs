using Refit;

namespace Rsp.IrasPortal.Infrastructure.HttpClients;

public interface IHealthCheckClient
{
    [Get("/health")]
    public Task<HttpResponseMessage> GetServiceHealth();
}