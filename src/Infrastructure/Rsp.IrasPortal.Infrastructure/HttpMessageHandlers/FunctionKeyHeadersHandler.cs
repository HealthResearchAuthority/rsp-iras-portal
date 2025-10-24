using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Infrastructure.HttpMessageHandlers;

/// <summary>
/// Delegating handler to add functions key header, before calling the function endpoint
/// </summary>
/// <seealso cref="DelegatingHandler" />
public class FunctionKeyHeadersHandler(AppSettings appSettings) : DelegatingHandler
{
    /// <summary>Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.</summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="request" /> was <see langword="null" />.</exception>
    /// <returns>The task object representing the asynchronous operation.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // the get the function from app settings
        var functionKey = appSettings.ProjectRecordValidationFunctionKey;

        if (!string.IsNullOrWhiteSpace(functionKey))
        {
            // no function key configured, skip adding header
            request.Headers.Add(RequestHeadersKeys.FunctionsKey, $"{functionKey}");
        }

        // Use the token to make the call.
        return await base.SendAsync(request, cancellationToken);
    }
}