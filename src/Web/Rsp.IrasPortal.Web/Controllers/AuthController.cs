using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Controllers;

// This controller handles authentication actions for the application.
// It uses attribute routing to map URLs like /Auth/SignIn and /Auth/Signout.
[Route("[controller]/[action]", Name = "auth:[action]")]
public class AuthController(AppSettings appSettings, IFeatureManager featureManager) : Controller
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
        // The SignOutResult below will handle federated sign-out
        // if configured. Signing out of the local cookie is sufficient, however
        // if we need to explicitly sign out from the external identity provider, then we need to
        // call the /logout url of the identity provider with the expected parameters.

        // following is an example of logout parameters

        //id_token_hint = eyJraWQiOiIxZTlnZGs3I...
        //&post_logout_redirect_uri = http://example-service.com/my-logout-url
        //&state = sadk8d4--lda % d

        var oneLoginEnabled = await featureManager.IsEnabledAsync(Features.OneLogin);

        var tokenHint = oneLoginEnabled ?
            await HttpContext.GetTokenAsync(ContextItemKeys.IdToken) :
            await HttpContext.GetTokenAsync(ContextItemKeys.AcessToken);

        var logoutBaseUrl = oneLoginEnabled ?
            $"{appSettings.OneLogin.Authority}/logout" :
            $"{appSettings.AuthSettings.LogoutUrl}";

        var redirectUri = Url.ActionLink("home", "researchaccount")?.TrimEnd('/');

        if (!oneLoginEnabled)
        {
            redirectUri = $"{redirectUri}/application/welcome";
        }

        var logoutUrl = string.IsNullOrWhiteSpace(tokenHint) ?
            redirectUri :
            $"{logoutBaseUrl}?id_token_hint={tokenHint}&post_logout_redirect_uri={redirectUri}";

        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme], new()
        {
            RedirectUri = logoutUrl
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