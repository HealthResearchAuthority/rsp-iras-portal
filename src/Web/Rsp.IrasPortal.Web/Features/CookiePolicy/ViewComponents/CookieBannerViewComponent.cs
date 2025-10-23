using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

namespace Rsp.IrasPortal.Web.Features.CookiePolicy.Components;

// The cookie value is not used — only checked for existence
// Safe: no user data is rendered or returned
public class CookieBannerViewComponent : ViewComponent
{
    private const string ViewPath = "~/Features/CookiePolicy/Views/CookieBanner.cshtml";

    public IViewComponentResult Invoke()
    {
        bool hasConsent = Request.Cookies.ContainsKey(CookieConsentNames.EssentialCookies);
        var bannerContent = (RichTextProperties?)ViewData[PageContentElements.CookieBannerContent];

        if (hasConsent)
            return Content(string.Empty); // Don't show banner again

        return View(ViewPath, bannerContent?.Value?.Markup);
    }
}