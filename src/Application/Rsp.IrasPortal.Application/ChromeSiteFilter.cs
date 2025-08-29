using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Application;

public class SiteChromeFilter(ICmsContentService contentService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            var footerData = await contentService.GetSiteFooter();

            if (footerData.IsSuccessStatusCode)
            {
                controller.ViewData["SiteFooter"] = footerData?.Content?.Properties.FooterLinks;
            }
        }

        await next();
    }
}