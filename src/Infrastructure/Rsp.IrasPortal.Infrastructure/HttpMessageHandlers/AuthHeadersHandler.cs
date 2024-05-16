using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Rsp.IrasPortal.Infrastructure.HttpMessageHandlers;

/// <summary>
/// Delegating handler to add authorization header, before calling external api
/// </summary>
/// <seealso cref="DelegatingHandler" />
public class AuthHeadersHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <summary>Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.</summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="request" /> was <see langword="null" />.</exception>
    /// <returns>The task object representing the asynchronous operation.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // do not create a private field for HttpContext
        var context = httpContextAccessor.HttpContext!;

        var bearerToken = await context.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");

        request.Headers.Add(HeaderNames.Authorization, $"Bearer {bearerToken}");

        // Use the token to make the call.
        return await base.SendAsync(request, cancellationToken);
    }
}