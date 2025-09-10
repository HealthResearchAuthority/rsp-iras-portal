using Microsoft.AspNetCore.WebUtilities;

namespace Rsp.IrasPortal.Infrastructure.HttpMessageHandlers;

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
        var uri = request.RequestUri;
        if (uri is not null)
        {
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("preview", out var previewValue))
            {
                // Add the Preview header if not already present
                if (!request.Headers.Contains("Preview"))
                {
                    request.Headers.Add("Preview", previewValue.ToString());
                }
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}