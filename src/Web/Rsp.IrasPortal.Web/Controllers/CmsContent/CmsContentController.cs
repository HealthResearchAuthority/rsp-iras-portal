﻿using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.Controllers.CmsContent;

public class CmsContentController(ICmsContentService cms) : Controller
{
    private static readonly string[] PathsToIgnore = { "jwks" };

    public async Task<IActionResult> Index()
    {
        // check if the current request requires CMS content in preview model
        var previewQuery = HttpContext?.Request?.Query["preview"];
        bool previewParsed = bool.TryParse(previewQuery, out var parsed);

        var path = HttpContext?.Request?.Path.Value?.Trim('/')?.ToLower();

        if (string.IsNullOrEmpty(path) || PathsToIgnore.Contains(path))
        {
            return NotFound();
        }

        var cmsPage = await cms.GetPageContentByUrl(path, parsed);

        if (!cmsPage.IsSuccessStatusCode || cmsPage.Content == null)
        {
            return NotFound();
        }

        ViewBag.Title = cmsPage.Content?.Name;

        return View("Index", cmsPage.Content);
    }
}