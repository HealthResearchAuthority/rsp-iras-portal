using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.ViewComponents;

public class LoginLandingPageViewComponent(ICmsContentService cms) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // check if the current request requires CMS content in preview model
        var previewQuery = HttpContext.Request.Query["preview"];
        bool previewParsed = bool.TryParse(previewQuery, out var parsed);

        var viewName = "~/Views/Shared/LoginLandingPageContent.cshtml";

        var landingPageContent = await cms.GetHomeContent(parsed);

        if (!landingPageContent.IsSuccessStatusCode || landingPageContent?.Content == null)
        {
            return View(viewName, new GenericPageResponse());
        }

        return View(viewName, landingPageContent.Content);
    }
}