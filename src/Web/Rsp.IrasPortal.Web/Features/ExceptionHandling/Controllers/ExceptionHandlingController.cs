using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Features.ExceptionHandling.Controllers;

[Route("error")]
public class ExceptionHandlingController(ILogger<ExceptionHandlingController> logger) : Controller
{
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("servererror")]
    public IActionResult Error()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("statuscode")]
    public IActionResult HandleStatusCode(int statusCode)
    {
        var path = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;

        // Try to get ProblemDetails from HttpContext.Items
        // the problem details will be set when returning StatusCode from this.ServerError extension method
        if (HttpContext.Items.TryGetValue(ContextItemKeys.ProblemDetails, out var problem) &&
            problem is ProblemDetails problemDetails)
        {
            var parameters =
                $"Title: {problemDetails.Title}, " +
                $"StatusCode: {problemDetails.Status}, " +
                $"Instance: {problemDetails.Instance ?? path}";

            logger.LogAsError(parameters, ErrorCodes.ERR_APP, problemDetails.Detail ?? "Unknown error as occured");

            return statusCode switch
            {
                StatusCodes.Status404NotFound => View("NotFound"),
                StatusCodes.Status403Forbidden => Forbidden(false),
                _ => Error()
            };
        }

        // fallback logging if no ProblemDetails, it also indicates that it's the direct request
        // not coming from the this.ServerError()
        if (statusCode is not StatusCodes.Status403Forbidden)
        {
            LogStatusError(ErrorCodes.ERR_STATUS, statusCode);
        }

        return statusCode switch
        {
            StatusCodes.Status404NotFound => View("NotFound"),
            StatusCodes.Status403Forbidden => Forbidden(),
            _ => Error()
        };
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("forbidden")]
    public IActionResult Forbidden(bool logError = true)
    {
        if (logError)
        {
            LogStatusError(ErrorCodes.ERR_FORBIDDEN, StatusCodes.Status403Forbidden);
        }

        return View();
    }

    private void LogStatusError(string errorCode, int statusCode)
    {
        var path = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;

        var parameters = $"StatusCode: {statusCode}, Instance: {path}";

        logger.LogAsError(parameters, errorCode, ReasonPhrases.GetReasonPhrase(statusCode));
    }
}