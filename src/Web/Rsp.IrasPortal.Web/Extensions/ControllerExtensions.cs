using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Web.Extensions;

public static class ControllerExtensions
{
    /// <summary>
    /// Transforms the ServiceResponse to the desired ActionResult{<typeparam name="T">}</typeparam>
    /// </summary>
    /// <param name="response">The response.</param>
    public static ObjectResult ServiceResult<T>(this ControllerBase controller, ServiceResponse<T> response)
    {
        if (response.IsSuccessStatusCode)
        {
            return new ObjectResult(response.Content) { StatusCode = (int)response.StatusCode };
        }

        var result = ProblemResult(controller, response);

        return new ObjectResult(result) { StatusCode = (int)response.StatusCode };
    }

    /// <summary>
    /// Transforms the non-generic ServiceResponse to IActionResult
    /// </summary>
    /// <param name="controller">Extended controller class</param>
    /// <param name="response">service response</param>
    public static ActionResult ServiceResult(this ControllerBase controller, ServiceResponse response)
    {
        if (response.IsSuccessStatusCode)
        {
            return controller.StatusCode((int)response.StatusCode);
        }

        return controller.StatusCode((int)response.StatusCode, response);
    }

    public static ProblemDetails ProblemResult(this ControllerBase controller, ServiceResponse response)
    {
        return new ProblemDetails
        {
            Title = response.ReasonPhrase,
            Detail = response.Error,
            Status = (int)response.StatusCode,
            Instance = controller.Request.Path
        };
    }
}