using System.Net;

namespace Rsp.IrasPortal.Application.Responses;

/// <summary>
/// Service response class to facilitate composing the API response, that doesn't require content, using the fluent pattern.
/// </summary>
/// <seealso cref="ServiceResponse" />
public class ServiceResponse
{
    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Gets a value indicating whether this instance is success status code.
    /// </summary>
    public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;

    /// <summary>
    /// Reason phrase for the error message.
    /// </summary>
    public string? ReasonPhrase { get; set; }

    /// <summary>
    /// Error message when the API fails.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Sets the StatusCode of the response
    /// </summary>
    /// <param name="statusCode">HttpStatusCode to set. Default is OK</param>
    public virtual ServiceResponse WithStatus(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        StatusCode = statusCode;

        return this;
    }

    /// <summary>
    /// Composes the error response with ErroMessage, ReasonPhrase and StatusCode
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="reasonPhrase">Reason phrase for the error</param>
    /// <param name="statusCode">HttpStatusCode to set. Default is OK</param>
    public virtual ServiceResponse WithError(string? errorMessage, string? reasonPhrase = null, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        Error = errorMessage;

        return WithStatus(statusCode).WithReason(reasonPhrase);
    }

    /// <summary>
    /// Sets the reason phrase of the response
    /// </summary>
    /// <param name="reasonPhrase">Reason phrase for the error</param>
    public virtual ServiceResponse WithReason(string? reasonPhrase)
    {
        ReasonPhrase = reasonPhrase;

        return this;
    }
}