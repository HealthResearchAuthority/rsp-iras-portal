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

    /// <summary>
    /// Header name for the functions key used to authenticate requests to Azure Functions
    /// </summary>
    public const string FunctionsKey = "x-functions-key";
}