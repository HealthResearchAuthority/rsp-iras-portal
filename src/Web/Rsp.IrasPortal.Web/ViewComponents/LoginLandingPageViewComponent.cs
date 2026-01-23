using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs.Responses.CmsContent;
using Rsp.Portal.Application.Services;

namespace Rsp.Portal.Web.ViewComponents;

public class LoginLandingPageViewComponent(ICmsContentService cms) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // check if the current request requires CMS content in preview model
        var isPreviewMode = false;
        if (HttpContext?.Request?.Query != null)
        {
            var previewQuery = HttpContext.Request.Query["preview"];
            bool.TryParse(previewQuery, out isPreviewMode);
        }

        var viewName = "~/Views/Shared/LoginLandingPageContent.cshtml";

        var landingPageContent = await cms.GetHomeContent(isPreviewMode);

        if (!landingPageContent.IsSuccessStatusCode || landingPageContent?.Content == null)
        {
            return View(viewName, new GenericPageResponse());
        }

        return View(viewName, landingPageContent.Content);
    }
}