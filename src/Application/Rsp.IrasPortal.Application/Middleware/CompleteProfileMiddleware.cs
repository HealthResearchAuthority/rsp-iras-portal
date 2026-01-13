using Microsoft.AspNetCore.Http;
using Rsp.Portal.Application.Constants;

namespace Rsp.Portal.Application.Middleware;

public class CompleteProfileMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Only redirect if the user is authenticated and flagged for completion
        if (context.User?.Identity?.IsAuthenticated == true &&
            context.Items.ContainsKey(ContextItemKeys.RequireProfileCompletion))
        {
            // Avoid redirect loops
            var path = context.Request.Path.Value?.ToLower();
            if (!string.IsNullOrEmpty(path) && !path.Contains("/profileandsettings/editprofile"))
            {
                // Response.Redirect call issues a new request
                // the CustomClaimsTransformation will be called again, where
                // it will fail the first login check because this session key was
                // already removed from there during the first login process.
                // we need to set a session key to indicate that this is the first login
                context.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

                // Also set a session key to indicate that profile creation is required
                // This session key will be used in EditProfile and Index views
                // to add query parameter `requireProfileCreation`. This parameter will be checked by
                // CustomClaimsTransformation when the user saves and edit profile for the first time
                // because all other if conditions will be false after the first login is completed.
                // When requireProfileCreation is true, will add an applicant claim temporarily
                // to satisfy the Workspace.Profile policy on ProfileAndSettings page
                context.Session.SetString(SessionKeys.RequireProfileCreation, bool.TrueString);

                context.Response.Redirect("/profileandsettings/editprofile");

                return;
            }
        }

        await next(context);
    }
}