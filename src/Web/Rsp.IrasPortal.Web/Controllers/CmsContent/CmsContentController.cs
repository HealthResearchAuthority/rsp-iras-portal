using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.Controllers.CmsContent;

public class CmsContentController(ICmsContentService cms) : Controller
{
    private static readonly string[] PathsToIgnore = { "jwks" };

    public async Task<IActionResult> Index()
    {
        var path = HttpContext?.Request?.Path.Value?.Trim('/')?.ToLower();

        if (string.IsNullOrEmpty(path) || PathsToIgnore.Contains(path))
        {
            return NotFound();
        }

        var cmsPage = await cms.GetPageContentByUrl(path);

        if (!cmsPage.IsSuccessStatusCode || cmsPage.Content == null)
        {
            return NotFound();
        }

        return View("Index", cmsPage.Content);
    }
}