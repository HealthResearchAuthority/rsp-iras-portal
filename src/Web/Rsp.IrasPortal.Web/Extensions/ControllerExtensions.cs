using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Web.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ServiceError<T>(this Controller controller, ServiceResponse<T> response)
    {
        // return the generic error page
        // if status is forbidden or not found
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => controller.Forbid(),
            HttpStatusCode.NotFound => controller.NotFound(),
            _ => controller.View("Error", ProblemResult(controller, response))
        };
    }

    public static IrasApplicationResponse GetApplicationFromSession(this Controller controller)
    {
        var context = controller.HttpContext;

        var application = context.Session.GetString(SessionKeys.Application);

        if (application != null)
        {
            return JsonSerializer.Deserialize<IrasApplicationResponse>(application)!;
        }

        return new IrasApplicationResponse();
    }

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
}