using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Web.Extensions;

public static class ControllerExtensions
{
    /// <summary>
    /// Returns an appropriate IActionResult based on the ServiceResponse status code.
    /// If Forbidden or NotFound, returns Forbid or NotFound result.
    /// Otherwise, returns the generic error view with problem details.
    /// </summary>
    public static IActionResult ServiceError<T>(this Controller controller, ServiceResponse<T> response)
    {
        // return the generic error page
        // if status is forbidden or not found
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => controller.Forbid(),
            _ => controller.View("Error", ProblemResult(controller, response))
        };
    }

    /// <summary>
    /// Returns an appropriate IActionResult based on the ServiceResponse status code.
    /// If Forbidden or NotFound, returns Forbid or NotFound result.
    /// Otherwise, returns the generic error view with problem details.
    /// </summary>
    public static IActionResult ServiceError(this Controller controller, ServiceResponse response)
    {
        // return the generic error page
        // if status is forbidden or not found
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => controller.Forbid(),
            _ => controller.View("Error", ProblemResult(controller, response))
        };
    }

    /// <summary>
    /// Retrieves the IrasApplicationResponse object from the session.
    /// Returns a new IrasApplicationResponse if not found in session.
    /// </summary>
    public static IrasApplicationResponse GetApplicationFromSession(this Controller controller)
    {
        var context = controller.HttpContext;

        var application = context.Session.GetString(SessionKeys.ProjectRecord);

        if (application != null)
        {
            return JsonSerializer.Deserialize<IrasApplicationResponse>(application)!;
        }

        return new IrasApplicationResponse();
    }

    /// <summary>
    /// Creates a ProblemDetails object from a ServiceResponse.
    /// Used for error reporting in views.
    /// </summary>
    public static ProblemDetails ProblemResult(this Controller controller, ServiceResponse response)
    {
        return new ProblemDetails
        {
            Title = response.ReasonPhrase,
            Detail = response.Error,
            Status = (int)response.StatusCode,
            Instance = controller.Request?.Path
        };
    }

    /// <summary>
    /// Extracts respondent information from the current HttpContext and user claims.
    /// </summary>
    public static RespondentDto GetRespondentFromContext(this Controller controller)
    {
        var httpContext = controller.HttpContext;
        var user = controller.User;

        return new RespondentDto
        {
            Id = httpContext.Items[ContextItemKeys.RespondentId]?.ToString() ?? string.Empty,
            EmailAddress = httpContext.Items[ContextItemKeys.Email]?.ToString() ?? string.Empty,
            GivenName = httpContext.Items[ContextItemKeys.FirstName]?.ToString() ?? string.Empty,
            FamilyName = httpContext.Items[ContextItemKeys.LastName]?.ToString() ?? string.Empty,
            Role = string.Join(',', user.Claims
                       .Where(claim => claim.Type == ClaimTypes.Role)
                       .Select(claim => claim.Value))
        };
    }
}