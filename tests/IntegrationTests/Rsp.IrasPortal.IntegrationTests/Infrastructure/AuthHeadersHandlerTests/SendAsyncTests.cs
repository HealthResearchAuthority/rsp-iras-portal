using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Infrastructure.HttpMessageHandlers;
using Rsp.Portal.IntegrationTests.Infrastructure;
using Shouldly;

namespace Rsp.Portal.IntegrationTests.Infrastructure.AuthHeadersHandlerTests;

public class SendAsyncTests
{
    [Fact]
    public async Task SendAsync_Should_Add_Authorization_Header_When_BearerToken_Is_Present()
    {
        // Arrange
        // this is a non-existent endpoint, which will result in 404
        // but the assertion is on the headers, not the end result
        var request = new HttpRequestMessage(HttpMethod.Get, "/");

        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddHttpContextAccessor();
                        services.AddTransient<TestHandler>();
                        services.AddTransient<AuthHeadersHandler>();

                        services
                            .AddHttpClient("TestHttpClient", // register httpclient with real innder and test outer message handler, using fake BaseAddress
                                client => client.BaseAddress = new Uri($"http://{Guid.NewGuid()}", UriKind.Absolute))
                            .AddHttpMessageHandler<AuthHeadersHandler>()
                            .AddHttpMessageHandler<TestHandler>(); // the outer handler will return the ok response, mimicing the downstream api call
                    })
                    .Configure(app =>
                    {
                        // register the path to use the inline middleware
                        app.Map("/headers-test", builder =>
                        {
                            builder.Run(async (ctx) =>
                            {
                                // get the IHttpClientFactory that was registered above
                                var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();

                                // get the registered httpclient
                                var client = factory.CreateClient("TestHttpClient");

                                // set the access token in the context
                                ctx.Items[ContextItemKeys.BearerToken] = "test-token";

                                // make a request using the fake base address above
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

        // the request message should have the headers
        var headers = request.Headers;
        headers.ShouldSatisfyAllConditions
        (
            () => headers.Contains(HeaderNames.Authorization).ShouldBeTrue(),
            () => headers.GetValues(HeaderNames.Authorization).First().ShouldBe("Bearer test-token")
        );
    }

    [Fact]
    public async Task SendAsync_Should_Not_Add_Authorization_Header_When_BearerToken_Is_Not_Present()
    {
        // Arrange
        // this is a non-existent endpoint, which will result in 404
        // but the assertion is on the headers, not the end result
        var request = new HttpRequestMessage(HttpMethod.Get, "/");

        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddHttpContextAccessor();
                        services.AddTransient<TestHandler>();
                        services.AddTransient<AuthHeadersHandler>();

                        services
                            .AddHttpClient("TestHttpClient", // register httpclient with real innder and test outer message handler, using fake BaseAddress
                                client => client.BaseAddress = new Uri($"http://{Guid.NewGuid()}", UriKind.Absolute))
                            .AddHttpMessageHandler<AuthHeadersHandler>()
                            .AddHttpMessageHandler<TestHandler>(); // the outer handler will return the ok response, mimicing the downstream api call
                    })
                    .Configure(app =>
                    {
                        // register the path to use the inline middleware
                        app.Map("/headers-test", builder =>
                        {
                            builder.Run(async (ctx) =>
                            {
                                // get the IHttpClientFactory that was registered above
                                var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();

                                // get the registered httpclient
                                var client = factory.CreateClient("TestHttpClient");

                                // make a request using the fake base address above
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

        // the request message should have the headers
        var headers = request.Headers;
        headers.ShouldSatisfyAllConditions
        (
            () => headers.Contains(HeaderNames.Authorization).ShouldBeFalse()
        );
    }
}