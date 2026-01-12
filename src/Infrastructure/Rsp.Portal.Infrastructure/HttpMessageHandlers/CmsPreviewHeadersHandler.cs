using Microsoft.AspNetCore.WebUtilities;

namespace Rsp.Portal.Infrastructure.HttpMessageHandlers;

/// <summary>
/// Delegating handler to add a Preview header when making calls to the CMS
/// </summary>
/// <seealso cref="DelegatingHandler" />
public class CmsPreviewHeadersHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Default = false
        var previewHeaderValue = "false";

        var uri = request.RequestUri;
        if (uri is not null)
        {
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("preview", out var previewValue))
            {
                // Only flip to true if the query string explicitly equals "true"
                if (string.Equals(previewValue.ToString(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    previewHeaderValue = "true";
                }
            }
        }

        // Always set (or overwrite) the Preview header
        request.Headers.Remove("Preview");
        request.Headers.Add("Preview", previewHeaderValue);

        return base.SendAsync(request, cancellationToken);
    }
}