using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Microsoft.Net.Http.Headers;
using Rsp.Portal.Application.Configuration;

namespace Rsp.Portal.Infrastructure.HttpMessageHandlers;

/// <summary>
/// Delegating handler to add functions key header, before calling the function endpoint
/// </summary>
/// <seealso cref="DelegatingHandler" />
[ExcludeFromCodeCoverage(Justification = "DefaultAzureCredential is not available locally")]
public class FunctionHeadersHandler(AppSettings appSettings) : DelegatingHandler
{
    /// <summary>Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.</summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="request" /> was <see langword="null" />.</exception>
    /// <returns>The task object representing the asynchronous operation.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // the get the function from app settings
        var scopes = appSettings.ProjectRecordValidationScopes;

        // This won't work locally, only in deployed environments with managed identity
        var credentials = new DefaultAzureCredential();

        var tokenRequestContext = new TokenRequestContext([scopes]);

        var accessToken = await credentials.GetTokenAsync(tokenRequestContext, cancellationToken);

        var bearerToken = accessToken.Token;

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            // no function key configured, skip adding header
            request.Headers.Add(HeaderNames.Authorization, $"Bearer {bearerToken}");
        }

        // Use the token to make the call.
        return await base.SendAsync(request, cancellationToken);
    }
}