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
        var authProperties = new AuthenticationProperties
        {
            RedirectUri = Url.RouteUrl("acc:home")
        };

        return new ChallengeResult(AuthSchemes.OpenIdConnect, authProperties);
    }

    /// <summary>
    /// // Signs the user out of the application.
    /// This method signs out the user from the local cookie authentication scheme.
    /// After sign-out, the user is redirected to the "acc:home" route.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> Signout()
    {
        var logoutUrl = await GetLogoutUrl();

        // Signs out the user from the local cookie authentication, and redirects
        // to external provider /logout endpoint
        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme], new()
        {
            RedirectUri = logoutUrl
        });
    }

    /// <summary>
    /// Signs the user out of the application.
    /// This method signs out the user from the local cookie authentication scheme.
    /// As session is timedout, the user is redirected to the "auth:sessiontimedout" route.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> TimedOut()
    {
        var logoutUrl = await GetLogoutUrl(true);

        // Signs out the user from the local cookie authentication, and redirects
        // to external provider /logout endpoint
        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme], new()
        {
            RedirectUri = logoutUrl
        });
    }

    [ExcludeFromCodeCoverage]
    public IActionResult SessionTimedOut()
    {
        // Returns a view indicating that the session has timed out.
        return View("_SessionTimedOut");
    }

    [ExcludeFromCodeCoverage]
    [NonAction]
    public async Task<string?> GetLogoutUrl(bool sessionTimedOut = false)
    {
        // The SignOutResult below will handle federated sign-out
        // if configured. Signing out of the local cookie is sufficient, however
        // if we need to explicitly sign out from the external identity provider, then we need to
        // call the /logout url of the identity provider with the expected parameters.

        // following is an example of logout parameters

        //id_token_hint = eyJraWQiOiIxZTlnZGs3I...
        //&post_logout_redirect_uri = http://example-service.com/my-logout-url
        //&state = sadk8d4--lda % d

        var oneLoginEnabled = await featureManager.IsEnabledAsync(FeatureFlags.OneLogin);

        var tokenHint = oneLoginEnabled ?
            await HttpContext.GetTokenAsync(ContextItemKeys.IdToken) :
            await HttpContext.GetTokenAsync(ContextItemKeys.AcessToken);

        var logoutBaseUrl = oneLoginEnabled ?
            $"{appSettings.OneLogin.Authority}/logout" :
            $"{appSettings.AuthSettings.LogoutUrl}";

        var redirectUri = sessionTimedOut switch
        {
            true => Url.ActionLink("SessionTimedOut", "Auth")?.TrimEnd('/'),
            false => Url.ActionLink("home", "researchaccount")?.TrimEnd('/')
        };

        // token hint shouldn't be null as a 60 second buffer
        // is added to the AuthCookieTimeout and SessionTimeout to grab the token
        return string.IsNullOrWhiteSpace(tokenHint) ?
            $"{logoutBaseUrl}?post_logout_redirect_uri={redirectUri}" :
            $"{logoutBaseUrl}?id_token_hint={tokenHint}&post_logout_redirect_uri={redirectUri}";
    }
}