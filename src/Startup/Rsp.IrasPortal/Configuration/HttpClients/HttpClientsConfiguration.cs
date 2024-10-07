using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Refit;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Infrastructure.HttpClients;
using Rsp.IrasPortal.Infrastructure.HttpMessageHandlers;

namespace Rsp.IrasPortal.Configuration.HttpClients;

[ExcludeFromCodeCoverage]
public static class HttpClientsConfiguration
{
    /// <summary>
    /// Adds the Orchestration service http clients
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors</param>
    /// <param name="appSettings">Application settings from appsettings.json</param>
    public static IServiceCollection AddHttpClients(this IServiceCollection services, AppSettings appSettings)
    {
        services
            .AddRestClient<IApplicationsHttpClient>()
            .ConfigureHttpClient(client => client.BaseAddress = appSettings.ApplicationsServiceUri)
            .AddHttpMessageHandler<AuthHeadersHandler>()
            .AddHeaderPropagation(options => options.Headers.Add(RequestHeadersKeys.CorrelationId));

        services
            .AddRestClient<IUserManagementServiceClient>()
            .ConfigureHttpClient(client => client.BaseAddress = appSettings.UsersServiceUri)
            .AddHttpMessageHandler<AuthHeadersHandler>()
            .AddHeaderPropagation(options => options.Headers.Add(RequestHeadersKeys.CorrelationId));

        services
           .AddRestClient<IQuestionSetServiceClient>()
           .ConfigureHttpClient(client => client.BaseAddress = appSettings.QuestionSetServiceUri)
           .AddHttpMessageHandler<AuthHeadersHandler>()
           .AddHeaderPropagation(options => options.Headers.Add(RequestHeadersKeys.CorrelationId));

        services
           .AddRestClient<IRespondentServiceClient>()
           .ConfigureHttpClient(client => client.BaseAddress = appSettings.ApplicationsServiceUri)
           .AddHttpMessageHandler<AuthHeadersHandler>()
           .AddHeaderPropagation(options => options.Headers.Add(RequestHeadersKeys.CorrelationId));

        return services;
    }

    /// <summary>
    /// Adds the rest client.
    /// </summary>
    /// <typeparam name="T">Interface to register as a Refit client</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors</param>
    public static IHttpClientBuilder AddRestClient<T>(this IServiceCollection services) where T : class
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(options),
            // Buffering enabled while we wait for this fix: https://github.com/reactiveui/refit/issues/1099
            // Otherwise the "Content-Length" won't be set and downstream requests with a body will fail
            Buffered = true
        };

        // add the http client, with retries, to the Ioc to call the down stream API
        return services
            .AddHttpClient(typeof(T).Name) // this name will be used as a SourceContext when logging request/response
            .AddTypedClient(client => RestService.For<T>(client, refitSettings));
    }
}