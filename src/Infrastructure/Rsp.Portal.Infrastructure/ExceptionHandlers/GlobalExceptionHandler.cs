using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rsp.Logging.Domain;
using Rsp.Logging.Extensions;

namespace Rsp.Portal.Infrastructure.ExceptionHandlers;

/// <summary>
/// Global exception handler registered in the ASP.NET Core pipeline.
/// Uses the primary constructor to receive an <see cref="ILogger{TCategoryName}"/>.
/// Returning <c>false</c> from <see cref="TryHandleAsync"/> defers response handling
/// to the next configured exception handler / built-in middleware.
/// </summary>
/// <remarks>
/// This handler's responsibility is limited to logging unhandled exceptions
/// in a consistent, structured manner (via <see cref="LogAsError"/>).
/// It intentionally does not modify the HTTP response so that higher-level
/// policies (e.g., problem details, developer exception page, etc.) can apply.
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle an unhandled exception.
    /// </summary>
    /// <param name="httpContext">Current HTTP request context.</param>
    /// <param name="exception">The exception that bubbled up.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>false</c> always – indicates this handler does not produce a response
    /// and allows subsequent handlers (or the default pipeline) to process the exception.
    /// </returns>
    /// <remarks>
    /// Only side-effect performed here is centralized structured logging.
    /// If in the future you need to short‑circuit and produce a standardized response
    /// (e.g., ProblemDetails), change the return value to <c>true</c> after writing the response.
    /// </remarks>
    public ValueTask<bool> TryHandleAsync
    (
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        // Log the exception with a domain-specific event code and description for better filtering / alerting.
        logger.LogAsError(LogEvents.UnhandledException.Code, LogEvents.UnhandledException.Description, exception);

        // Returning false delegates further handling to other registered exception handlers / middleware.
        return ValueTask.FromResult(false);
    }
}