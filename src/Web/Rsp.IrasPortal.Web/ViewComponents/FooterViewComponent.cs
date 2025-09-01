using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.ViewComponents;

public class FooterViewComponent(ICmsContentService contentService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var footerData = await contentService.GetSiteSettings();

        if (!footerData.IsSuccessStatusCode || footerData.Content == null)
        {
            return View("~/Views/Shared/Footer.cshtml", new List<LinkModel>());
        }

        return View("~/Views/Shared/Footer.cshtml", footerData.Content.FooterLinks);
    }
}