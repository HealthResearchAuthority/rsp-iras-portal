using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.ServiceClients;

namespace Rsp.IrasPortal.Web.ViewComponents;

public class FooterViewComponent(ICmsContentServiceClient cms) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var footerData = await cms.GetSiteSettings();

        if (!footerData.IsSuccessful)
        {
            throw new NotImplementedException();
        }

        return View("~/Views/Shared/Footer.cshtml", footerData.Content.Properties.FooterLinks);
    }
}