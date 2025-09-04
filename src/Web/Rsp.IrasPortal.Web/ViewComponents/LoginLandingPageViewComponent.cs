using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.ViewComponents;

public class LoginLandingPageViewComponent(ICmsContentService cms) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var viewName = "~/Views/Shared/LoginLandingPageContent.cshtml";

        var landingPageContent = await cms.GetHomeContent();

        if (!landingPageContent.IsSuccessStatusCode || landingPageContent?.Content?.Properties?.LoginLandingPageContent == null)
        {
            return View(viewName, new PageContent());
        }

        return View(viewName, landingPageContent.Content.Properties.LoginLandingPageContent);
    }
}