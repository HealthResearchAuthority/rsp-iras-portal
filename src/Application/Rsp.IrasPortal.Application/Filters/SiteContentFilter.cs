using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Application.Filters;

public class SiteContentFilter(ICmsContentService contentService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // check if the current request requires CMS content in preview model
        var previewQuery = context.HttpContext.Request.Query["preview"];
        bool previewParsed = bool.TryParse(previewQuery, out var parsed);

        // for every controller request get the page content
        // this includes the page specific content as well as the site footer
        if (context.Controller is Controller controller)
        {
            var isCmsContentController = controller.GetType().Name == "CmsContentController";
            var path = context.HttpContext.Request.Path;

            if (!isCmsContentController)
            {
                // do not try and retrieve mixed content when the page reqested is a generic content page
                var pageContent = await contentService.GetMixedPageContentByUrl(path, parsed);

                if (pageContent.IsSuccessStatusCode && pageContent.Content != null)
                {
                    controller.ViewData["PageContent"] = pageContent.Content.ContentItems;
                }
            }

            var footerData = await contentService.GetSiteSettings(parsed);

            if (footerData.IsSuccessStatusCode)
            {
                controller.ViewData["SiteFooter"] = footerData?.Content?.FooterLinks;
            }
        }

        await next();
    }
}