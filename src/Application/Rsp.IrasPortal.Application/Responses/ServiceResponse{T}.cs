using System.Net;

namespace Rsp.Portal.Application.Responses;

/// <summary>
/// A generic service response class to facilitate composing the API response using the fluent pattern.
/// </summary>
/// <typeparam name="T"><see cref="T"/></typeparam>
/// <seealso cref="ServiceResponse" />
public class ServiceResponse<T> : ServiceResponse
{
    public T? Content { get; set; }

    /// <summary>
    /// Sets the Content of the response
    /// </summary>
    /// <param name="content"><see cref="T"/></param>
    /// <param name="statusCode">HttpStatusCode to set. Default is OK</param>
    public ServiceResponse<T> WithContent(T content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Content = content;
        StatusCode = statusCode;

        return this;
    }

    /// <summary>
    /// Sets the StatusCode of the response
    /// </summary>
    /// <param name="statusCode">HttpStatusCode to set. Default is OK</param>
    public override ServiceResponse<T> WithStatus(HttpStatusCode statusCode = HttpStatusCode.OK)
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
    public override ServiceResponse<T> WithError(string? errorMessage, string? reasonPhrase = null, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        Error = errorMessage;
        ReasonPhrase = reasonPhrase;
        StatusCode = statusCode;

        return this;
    }

    /// <summary>
    /// Sets the reason phrase of the response
    /// </summary>
    /// <param name="reasonPhrase">Reason phrase for the error</param>
    public override ServiceResponse<T> WithReason(string? reasonPhrase)
    {
        ReasonPhrase = reasonPhrase;

        return this;
    }
}