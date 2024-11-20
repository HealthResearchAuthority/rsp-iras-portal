using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "auth:[action]")]
public class AuthController(ILogger<ApplicationController> logger) : Controller
{
    public IActionResult SignIn()
    {
        logger.LogInformationHp("called");

        return new ChallengeResult("OpenIdConnect", new()
        {
            RedirectUri = Url.RouteUrl("app:welcome")
        });
    }

    public async Task<IActionResult> Signout()
    {
        logger.LogInformationHp("called");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync("OpenIdConnect");

        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme, "OpenIdConnect"], new()
        {
            RedirectUri = Url.RouteUrl("app:welcome")
        });
    }
}