namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// RequestHeader Keys. These keys are used
/// to lookup headers in HttpContext.Request.Headers
/// </summary>
public struct RequestHeadersKeys
{
    /// <summary>
    /// Header name for unique identifier assigned to a particular request
    /// </summary>
    public const string CorrelationId = "x-correlation-id";
}