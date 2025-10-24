using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Infrastructure.HttpMessageHandlers;
using Shouldly;

namespace Rsp.IrasPortal.IntegrationTests.Infrastructure.FunctionKeyHeadersHandlerTests;

public class SendAsyncTests
{
    [Fact]
    public async Task SendAsync_Should_Add_FunctionsKey_Header_When_FunctionKey_Is_Present()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/");

        var host = new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddTransient<TestHandler>();
                    services.AddTransient<FunctionKeyHeadersHandler>();

                    // Register AppSettings with a function key value
                    services.AddSingleton(new AppSettings
                    {
                        ProjectRecordValidationFunctionKey = "test-func-key"
                    });

                    services
                       .AddHttpClient("TestHttpClient", client =>
                            client.BaseAddress = new Uri($"http://{Guid.NewGuid()}", UriKind.Absolute))
                       .AddHttpMessageHandler<FunctionKeyHeadersHandler>()
                       .AddHttpMessageHandler<TestHandler>(); // outer handler returns OK, mimicking downstream API
                })
                .Configure(app =>
                {
                    app.Map("/headers-test", builder =>
                    {
                        builder.Run(async ctx =>
                        {
                            var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
                            var client = factory.CreateClient("TestHttpClient");
                            await client.SendAsync(request);
                        });
                    });
                });
        }).Build();

        await host.StartAsync();

        // Act
        var response = await host.GetTestClient().GetAsync("/headers-test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var headers = request.Headers;
        headers.ShouldSatisfyAllConditions
            (
                () => headers.Contains(RequestHeadersKeys.FunctionsKey).ShouldBeTrue(),
                () => headers.GetValues(RequestHeadersKeys.FunctionsKey).First().ShouldBe("test-func-key")
            );
    }

    [Fact]
    public async Task SendAsync_Should_Not_Add_FunctionsKey_Header_When_FunctionKey_Is_Not_Present()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/");

        var host = new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddTransient<TestHandler>();
                    services.AddTransient<FunctionKeyHeadersHandler>();

                    // Function key not configured
                    services.AddSingleton(new AppSettings());

                    services
                        .AddHttpClient("TestHttpClient",
                        client => client.BaseAddress = new Uri($"http://{Guid.NewGuid()}", UriKind.Absolute))
                        .AddHttpMessageHandler<FunctionKeyHeadersHandler>()
                        .AddHttpMessageHandler<TestHandler>();
                })
                .Configure(app =>
                {
                    app.Map("/headers-test", builder =>
                    {
                        builder.Run(async ctx =>
                        {
                            var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
                            var client = factory.CreateClient("TestHttpClient");
                            await client.SendAsync(request);
                        });
                    });
                });
        }).Build();

        await host.StartAsync();

        // Act
        var response = await host.GetTestClient().GetAsync("/headers-test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var headers = request.Headers;
        headers.ShouldSatisfyAllConditions
        (
            () => headers.Contains(RequestHeadersKeys.FunctionsKey).ShouldBeFalse()
        );
    }

    private class TestHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}