using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Application.Filters;

public class SiteContentFilter(ICmsContentService contentService,
    LinkGenerator linkGenerator) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // check if the current request requires CMS content in preview model
        var previewQuery = context.HttpContext.Request.Query["preview"];
        bool previewParsed = bool.TryParse(previewQuery, out var isPreview);

        // for every controller request get the page content
        // this includes the page specific content as well as the site footer
        if (context.Controller is Controller controller)
        {
            var isCmsContentController = controller.GetType().Name == "CmsContentController";
            var path = context.HttpContext.Request.Path;

            // do not try and retrieve mixed content when the page reqested is a generic content page
            if (!isCmsContentController)
            {
                var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
                if (controllerActionDescriptor != null && context.HttpContext.Request.Method.ToLower() != "get")
                {
                    // Access the method info
                    var methodInfo = controllerActionDescriptor.MethodInfo;

                    // Get your custom attribute that holds the CMS url path
                    var cmsContentAttribute = methodInfo.GetCustomAttribute<CmsContentActionAttribute>();

                    if (cmsContentAttribute != null)
                    {
                        var controllerName = controller.GetType().Name.Replace("Controller", "");
                        var actionUrl = linkGenerator.GetPathByAction(cmsContentAttribute.ActionName, controllerName);

                        // replace the request path with the path of the Action specified in the custom attribute
                        if (actionUrl != null)
                        {
                            path = actionUrl;
                        }
                    }
                }

                ServiceResponse<MixedContentPageResponse> pageContent = null!;

                if (path == "/")
                {
                    // query the homepage to get the dashboard content
                    pageContent = await contentService.GetDashboardContent(isPreview);
                }
                else
                {
                    pageContent = await contentService.GetMixedPageContentByUrl(path, isPreview);
                }

                if (pageContent.IsSuccessStatusCode && pageContent.Content != null)
                {
                    controller.ViewData["PageContent"] = pageContent.Content.ContentItems;
                }
            }

            var siteSettings = await contentService.GetSiteSettings(isPreview);

            if (siteSettings.IsSuccessStatusCode)
            {
                controller.ViewData["SiteFooter"] = siteSettings?.Content?.FooterLinks;
                controller.ViewData["PhaseBanner"] = siteSettings?.Content?.PhaseBannerContent;
                controller.ViewData["ServiceNavigation"] = siteSettings?.Content?.ServiceNavigation;
            }
        }

        await next();
    }
}