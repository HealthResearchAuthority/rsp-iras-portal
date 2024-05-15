using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;

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

    public async Task<IActionResult> Signout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync("OpenIdConnect");

        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme, "OpenIdConnect"], new()
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

    public IActionResult StartNewApplication()
    {
        HttpContext.Session.Remove("Id");
        HttpContext.Session.Remove("ProjectName");
        HttpContext.Session.Remove("OfficeLocation");
        HttpContext.Session.Remove("ApplicationType");
        HttpContext.Session.Remove("ProjectCategory");

        return RedirectToAction(nameof(ProjectName));
    }

    public async Task<IActionResult> LoadExistingApplication(string applicationIdSelect)
    {
        logger.LogInformation(applicationIdSelect);
        if (applicationIdSelect == null) return RedirectToAction(nameof(Welcome));

        var application = await applicationsService.GetApplication(Int32.Parse(applicationIdSelect));
        if (application != null)
        {
            HttpContext.Session.SetString("Id", applicationIdSelect.ToString());
            HttpContext.Session.SetString("ProjectName", application.Title ?? "");
            HttpContext.Session.SetString("OfficeLocation", ((int)application.Location).ToString() ?? "");
            HttpContext.Session.SetString("ApplicationType", String.Join(",", application.ApplicationCategories ?? []));
            HttpContext.Session.SetString("ProjectCategory", application.ProjectCategory ?? "");

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
        HttpContext.Session.SetString("OfficeLocation", officeLocation ?? "");
        return RedirectToAction(nameof(ApplicationType));
    }

    public async Task<IActionResult> ApplicationType()
    {
        var categories = await categoriesService.GetApplicationCategories();
        ViewData["ApplicationType"] = HttpContext.Session.GetString("ApplicationType")?.Split(",").ToList() ?? [];
        return View(categories);
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
        return View(GetApplicationFromSession());
    }

    [HttpPost]
    public async Task<IActionResult> SaveDraftApplication()
    {
        IrasApplication createdApplication;

        var id = HttpContext.Session.GetString("Id");
        var application = GetApplicationFromSession();

        createdApplication = id == null ?
            await applicationsService.CreateApplication(application) :
            await applicationsService.UpdateApplication(Int32.Parse(id), application);

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

    private IrasApplication GetApplicationFromSession()
    {
        Location? location = null;
        if (int.TryParse(HttpContext.Session.GetString("OfficeLocation"), out int parsedLocation)) location = (Location?)parsedLocation;

        return new IrasApplication
        {
            Title = HttpContext.Session.GetString("ProjectName"),
            Location = location,
            ApplicationCategories = HttpContext.Session.GetString("ApplicationType")?.Split(",").ToList() ?? [],
            ProjectCategory = HttpContext.Session.GetString("ProjectCategory") ?? "",
            StartDate = DateTime.Parse(HttpContext.Session.GetString("ProjectStartDate") ?? DateTime.Now.ToString()),
        };
    }
}