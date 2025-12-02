using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Features.CookiePolicy.Controllers;

[Route("[controller]/[action]", Name = "cookies:[action]")]
public class CookiesController : Controller
{
    [HttpPost]
    public IActionResult AcceptConsent(string? additionalConsent, string settingsSource)
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
        var analyticsValue = additionalConsent == CookieConsentValues.Yes ? "true" : "false";

        // clear existing analytics cookies if user rejected analytics consent
        if (analyticsValue != CookieConsentValues.Yes && Request?.Cookies?.Keys != null)
        {
            // loop over existing analytics cookies and delete them from the response
            foreach (var cookie in Request.Cookies.Keys)
            {
                if (AnalyticsCookies.AnalyticsCookiePrefixes.Any(prefix => cookie.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    Response.Cookies.Delete(cookie);
                }
            }
        }

        Response.Cookies.Append(CookieConsentNames.AdditionalCookies, analyticsValue, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        // set notification banner so it shows on the page
        if (settingsSource == CookieConfirmationSource.CookieSettingsPage)
        {
            TempData[TempDataKeys.ShowCookiesSavedNotificationBanner] = true;
        }

        if (settingsSource == CookieConfirmationSource.CookieBanner)
        {
            TempData[TempDataKeys.ShowCookiesSavedHeaderBanner] = true;
        }

        // Redirect back to referring page
        var referer = Request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer)
            ? Redirect(referer)
            : RedirectToAction("Index", "Home");
    }

    [HttpGet("/pages/cookies")]
    public IActionResult CookieSettings()
    {
        return View("~/Features/CookiePolicy/Views/CookiesSettingsPage.cshtml");
    }

    [HttpPost]
    public IActionResult HideCookieSuccessBanner()
    {
        var referer = Request.Headers.Referer.ToString();
        return !string.IsNullOrEmpty(referer)
            ? Redirect(referer)
            : RedirectToAction("Index", "Home");
    }
}