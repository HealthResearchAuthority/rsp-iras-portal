using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Features.CookiePolicy.Components;

public class CookieBannerViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        bool hasConsent = Request.Cookies.ContainsKey(CookieConsentNames.EssentialCookies);

        if (hasConsent)
            return Content(string.Empty); // Don't show banner again

        return View("~/Features/CookiePolicy/Views/CookieBanner.cshtml");
    }
}