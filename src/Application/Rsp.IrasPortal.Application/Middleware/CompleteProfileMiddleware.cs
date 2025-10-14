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

            var email = context.Items[ContextItemKeys.Email];
            var phone = context.Items["telephoneNumber"]?.ToString();
            var identityProviderId = context.Items["identityProviderId"];

            // Avoid redirect loops
            var path = context.Request.Path.Value?.ToLower();
            if (!string.IsNullOrEmpty(path) && !path.Contains("profileandsettings/completeprofile"))
            {
                var encodedPhone = string.Empty;

                var urlParameters = string.Join("&", $"email={email}", $"identityProviderId={identityProviderId}");

                // URL encode the telephone in case it has + symbol
                if (!string.IsNullOrEmpty(phone))
                {
                    encodedPhone = Uri.EscapeDataString(phone);

                    urlParameters = string.Join("&", $"telephone={encodedPhone}", urlParameters);
                }

                context.Response.Redirect($"/profileandsettings/completeprofile?{urlParameters}");

                return;
            }
        }
        await next(context);
    }
}