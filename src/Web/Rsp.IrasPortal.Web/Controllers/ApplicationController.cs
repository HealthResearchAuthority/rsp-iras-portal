using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]")]
public class ApplicationController(ILogger<ApplicationController> logger, ICategoriesService categoriesService, IApplicationsService applicationsService) : Controller
{
    public IActionResult SignIn()
    {
        return new ChallengeResult("OpenIdConnect", new()
        {
            RedirectUri = Url.Action(nameof(Welcome))
        });
    }

    [Route("/")]
    public async Task<IActionResult> Welcome()
    {
        var applications = await applicationsService.GetApplications();
        return View(nameof(Index), applications);
    }

    public async Task<IActionResult> LoadApplication(int id)
    {
        var application = await applicationsService.GetApplication(5);
        if (application != null)
        {
            HttpContext.Session.SetString("Id", id.ToString());
            HttpContext.Session.SetString("ProjectName", application.Title);
            logger.LogInformation(application.Title);
            HttpContext.Session.SetString("OfficeLocation", ((int)application.Location).ToString());
            logger.LogInformation(application.Location.ToString());
            HttpContext.Session.SetString("ApplicationType", application.ApplicationCategories.ToString());
            logger.LogInformation(application.ApplicationCategories.ToString());
            HttpContext.Session.SetString("ProjectCategory", application.ProjectCategory);
            logger.LogInformation(application.ProjectCategory);

            return RedirectToAction(nameof(ProjectName));
        }

        var applications = await applicationsService.GetApplications();

        return RedirectToAction(nameof(Welcome));
    }

    [Authorize(Policy = "IsAdmin")]
    public IActionResult ProjectName()
    {
        return View(nameof(ProjectName), HttpContext.Session.GetString("ProjectName") ?? "");
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
        HttpContext.Session.SetString("ProjectName", projectName ?? "");
        return RedirectToAction(nameof(Country));
    }

    public IActionResult Country()
    {
        return View(nameof(Country), HttpContext.Session.GetString("OfficeLocation") ?? "");
    }

    [HttpPost]
    public IActionResult SaveCountry(string officeLocation)
    {
        HttpContext.Session.SetString("OfficeLocation", officeLocation ?? "0");
        return RedirectToAction(nameof(ApplicationType));
    }

    public async Task<IActionResult> ApplicationType()
    {
        try
        {
            var categories = await categoriesService.GetApplicationCategories();
            ViewData["ApplicationType"] = HttpContext.Session.GetString("ApplicationType") ?? "";
            return View(categories);
        }
        catch
        {
            return RedirectToAction(nameof(Error));
        }
    }

    [HttpPost]
    public IActionResult SaveApplicationType(string applicationType)
    {
        HttpContext.Session.SetString("ApplicationType", applicationType ?? "");
        return RedirectToAction(nameof(ProjectCategory));
    }

    public async Task<IActionResult> ProjectCategory()
    {
        try
        {
            var categories = await categoriesService.GetProjectCategories();
            ViewData["ProjectCategory"] = HttpContext.Session.GetString("ProjectCategory") ?? "";
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
        HttpContext.Session.SetString("ProjectCategory", projectCategory ?? "");
        return RedirectToAction(nameof(ReviewAnswers));
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
        var application = new IrasApplication
        {
            Title = HttpContext.Session.GetString("ProjectName"),
            Location = (Location)Int32.Parse(HttpContext.Session.GetString("OfficeLocation") ?? "0"),
            ApplicationCategories = [HttpContext.Session.GetString("ApplicationType") ?? ""],
            ProjectCategory = HttpContext.Session.GetString("ProjectCategory") ?? "",
            StartDate = DateTime.Parse(HttpContext.Session.GetString("ProjectStartDate") ?? DateTime.Now.ToString()),
        };

        return View(application);
    }

    [HttpPost]
    public async Task<IActionResult> SaveDraftApplication()
    {
        var application = new IrasApplication
        {
            Title = HttpContext.Session.GetString("ProjectName"),
            Location = (Location)Int32.Parse(HttpContext.Session.GetString("OfficeLocation") ?? "0"),
            ApplicationCategories = [HttpContext.Session.GetString("ApplicationType") ?? ""],
            ProjectCategory = HttpContext.Session.GetString("ProjectCategory") ?? "",
            StartDate = DateTime.Parse(HttpContext.Session.GetString("ProjectStartDate") ?? DateTime.Now.ToString()),
        };

        var createdApplication = await applicationsService.CreateApplication(application);

        return RedirectToAction(nameof(DraftSaved), createdApplication);
    }

    public IActionResult DraftSaved(IrasApplication application)
    {
        return View(application);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}