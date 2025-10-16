using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Application.Middleware;

public class CompleteProfileMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Only redirect if the user is authenticated and flagged for completion
        if (context.User?.Identity?.IsAuthenticated == true &&
            context.Items.ContainsKey(ContextItemKeys.RequireProfileCompletion))
        {
            // remove the RequireProfileCompletion flag before redirecting
            // the user to complete profile page
            context.Items.Remove(ContextItemKeys.RequireProfileCompletion);

            // Avoid redirect loops
            var path = context.Request.Path.Value?.ToLower();
            if (!string.IsNullOrEmpty(path) && !path.Contains("/profileandsettings/editprofile"))
            {
                context.Response.Redirect($"/profileandsettings/editprofile");

                return;
            }
        }
        await next(context);
    }
}