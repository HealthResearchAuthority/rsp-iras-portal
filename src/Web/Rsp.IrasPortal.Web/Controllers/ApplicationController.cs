using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]")]
public class ApplicationController(ILogger<ApplicationController> logger, ICategoriesService categoriesService) : Controller
{
    public IActionResult SignIn()
    {
        return new ChallengeResult("OpenIdConnect", new()
        {
            RedirectUri = Url.Action(nameof(Welcome))
        });
    }

    [Route("/")]
    public IActionResult Welcome()
    {
        return View("Index");
    }

    [Authorize(Policy = "IsAdmin")]
    public IActionResult ProjectName()
    {
        return View();
    }

    public async Task<IActionResult> Signout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync("OpenIdConnect");

        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme, "OpenIdConnect"], new()
        {
            RedirectUri = Url.Action(nameof(Welcome))
        });
    }

    [HttpPost]
    public IActionResult SaveProjectName(string projectName)
    {
        ViewData["ProjectName"] = projectName;
        return RedirectToAction(nameof(Country));
    }

    public IActionResult Country()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SaveCountry(string officeLocation)
    {
        logger.LogMethodStarted(LogLevel.Information);

        ViewData["Country"] = officeLocation;
        return RedirectToAction(nameof(ApplicationType));
    }

    public async Task<IActionResult> ApplicationType()
    {
        var categories = await categoriesService.GetApplicationCategories();
        return View(categories);
    }

    [HttpPost]
    public IActionResult SaveApplicationType(string applicationType)
    {
        ViewData["ApplicationType"] = applicationType;
        return RedirectToAction(nameof(ProjectCategory));
    }

    public async Task<IActionResult> ProjectCategory()
    {
        try
        {
            var categories = await categoriesService.GetProjectCategories();
            return View(categories);
        }
        catch
        {
            return RedirectToAction(nameof(Error));
        }
    }

    [HttpPost]
    public IActionResult SaveProjectCategory(string projectCategory)
    {
        ViewData["ProjectCategory"] = projectCategory;
        return RedirectToAction(nameof(ProjectStartDate));
    }

    public IActionResult ProjectStartDate()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SaveProjectStartDate(string projectStartDate)
    {
        ViewData["ProjectStartDate"] = projectStartDate;
        return RedirectToAction(nameof(DocumentUpload));
    }

    public IActionResult DocumentUpload()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SaveDocumentUpload(string supportingDocument)
    {
        ViewData["SupportingDocument"] = supportingDocument;
        return RedirectToAction(nameof(ReviewAnswers));
    }

    public IActionResult ReviewAnswers()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}