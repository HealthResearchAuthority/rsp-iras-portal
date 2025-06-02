using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Controllers;

// This controller handles authentication actions for the application.
// It uses attribute routing to map URLs like /Auth/SignIn and /Auth/Signout.
[Route("[controller]/[action]", Name = "auth:[action]")]
public class AuthController : Controller
{
    // Initiates the OpenID Connect authentication challenge.
    // When a user accesses /Auth/SignIn, this method triggers the OIDC middleware,
    // redirecting the user to the identity provider's login page.
    // After successful authentication, the user is redirected to the "acc:home" route.
    public IActionResult SignIn()
    {
        return new ChallengeResult(AuthSchemes.OpenIdConnect, new()
        {
            RedirectUri = Url.RouteUrl("acc:home")
        });
    }

    // Signs the user out of the application.
    // This method signs out the user from the local cookie authentication scheme.
    // After sign-out, the user is redirected to the "acc:home" route.
    public async Task<IActionResult> Signout()
    {
        // Signs out the user from the local cookie authentication.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // The SignOutResult below can handle federated sign-out
        // if configured, and in many Razor Pages scenarios, signing out of the local cookie is sufficient.
        // If we need to explicitly sign out from the external identity provider, then we need to
        // call the HttpContext.SignOutAsync method with the OpenIdConnect scheme.

        // Returns a sign-out result, which will clear the authentication cookie and redirect.
        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme], new()
        {
            RedirectUri = Url.RouteUrl("acc:home")
        });
    }

    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> TimedOut()
    {
        // Signs out the user from the local cookie authentication.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Returns a view indicating that the session has timed out.
        return View("_SessionTimedOut");
    }
}