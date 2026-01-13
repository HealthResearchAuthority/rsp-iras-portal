using System.Net;
using System.Text;

namespace Rsp.Portal.IntegrationTests.Infrastructure;

/// <summary>
/// Test DelegatingHandler that will be used as an inner handler of the real message handler
/// </summary>
/// <seealso cref="DelegatingHandler" />
public class TestHandler : DelegatingHandler
{
    /// <summary>Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.</summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="request" /> was <see langword="null" />.</exception>
    /// <returns>The task object representing the asynchronous operation.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("OK", Encoding.UTF8, "application/json")
        });
}