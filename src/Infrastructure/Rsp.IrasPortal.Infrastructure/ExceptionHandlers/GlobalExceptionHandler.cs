using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Rsp.IrasPortal.Infrastructure.ExceptionHandlers;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly LinkGenerator _linkGenerator;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, LinkGenerator linkGenerator)
    {
        _logger = logger;
        _linkGenerator = linkGenerator;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // There is different handler for Not Found exception
        if (httpContext.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            return false;
        }

        var exceptionId = Guid.NewGuid().ToString();

        var problemDetails = new ProblemDetails
        {
            Title = "Unexpected error occurred",
            Detail = exception.Message,
            Status = httpContext.Response.StatusCode,
            Instance = httpContext.Request?.Path
        };

        _logger.LogError(exception, "Unhandled exception occurred. ExceptionId: {ExceptionId}, ProblemDetails: {@ProblemDetails}", exceptionId, problemDetails);

        var redirectUrl = _linkGenerator.GetPathByRouteValues(
            httpContext,
            routeName: "exc:Index",
            values: new { exceptionId });

        httpContext.Response.Redirect(redirectUrl!);

        return true;
    }
}