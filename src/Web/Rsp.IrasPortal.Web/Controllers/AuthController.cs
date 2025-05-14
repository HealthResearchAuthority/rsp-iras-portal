using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "auth:[action]")]
public class AuthController : Controller
{
    public IActionResult SignIn()
    {
        return new ChallengeResult(AuthSchemes.OpenIdConnect, new()
        {
            RedirectUri = Url.RouteUrl("acc:home")
        });
    }

    public async Task<IActionResult> Signout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(AuthSchemes.OpenIdConnect);

        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme, AuthSchemes.OpenIdConnect], new()
        {
            RedirectUri = Url.RouteUrl("acc:home")
        });
    }
}