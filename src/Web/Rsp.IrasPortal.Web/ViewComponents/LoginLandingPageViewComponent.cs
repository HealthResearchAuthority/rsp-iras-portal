using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.ServiceClients;

namespace Rsp.IrasPortal.Web.ViewComponents;

public class LoginLandingPageViewComponent(ICmsContentServiceClient cms) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var footerData = await cms.GetSiteSettings();

        if (!footerData.IsSuccessful)
        {
            throw new NotImplementedException();
        }

        return View("~/Views/Shared/LoginLandingPageContent.cshtml", footerData.Content.Properties.LoginLandingPageBodyText);
    }
}