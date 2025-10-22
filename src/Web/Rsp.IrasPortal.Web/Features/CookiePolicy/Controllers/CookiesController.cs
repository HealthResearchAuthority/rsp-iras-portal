using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Features.CookiePolicy.Controllers;

[Route("[controller]/[action]", Name = "cookies:[action]")]
public class CookiesController : Controller
{
    [HttpPost]
    public IActionResult AcceptConsent(string? additionalConsent)
    {
        // Always set the main consent cookie
        Response.Cookies.Append(CookieConsentNames.EssentialCookies, "true", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        // Essential cookies are always enabled, so no need to record separately
        // But we’ll track analytics preference
        var analyticsValue = additionalConsent == "yes" ? "true" : "false";

        Response.Cookies.Append(CookieConsentNames.AdditionalCookies, analyticsValue, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        // Redirect back to referring page
        var referer = Request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer)
            ? Redirect(referer)
            : RedirectToAction("Index", "Home");
    }

    [HttpGet("pages/cookies/")]
    public IActionResult CookieSettings()
    {
        return View("~/Features/CookiePolicy/Views/CookiesSettingsPage.cshtml");
    }
}