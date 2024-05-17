using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Extensions;
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
        HttpContext.Session.RemoveAllSessionValues();

        return RedirectToAction(nameof(ProjectName));
    }

    public async Task<IActionResult> LoadExistingApplication(string applicationIdSelect)
    {
        if (applicationIdSelect == null)
        {
            return RedirectToAction(nameof(Welcome));
        }

        var applicationId = Int32.Parse(applicationIdSelect);
        var application = await applicationsService.GetApplication(applicationId);

        if (application != null)
        {
            HttpContext.Session.SetInt32(SessionConstants.Id, applicationId);
            HttpContext.Session.SetString(SessionConstants.Title, application.Title ?? "");
            HttpContext.Session.SetInt32(SessionConstants.Country, application.Location != null ? (int)application.Location : -1);
            HttpContext.Session.SetString(SessionConstants.ApplicationType, String.Join(",", application.ApplicationCategories ?? []));
            HttpContext.Session.SetString(SessionConstants.ProjectCategory, application.ProjectCategory ?? "");

            return RedirectToAction(nameof(ProjectName));
        }

        var applications = await applicationsService.GetApplications();

        return RedirectToAction(nameof(Welcome));
    }

    [Authorize(Policy = "IsAdmin")]
    public IActionResult ProjectName()
    {
        return View(nameof(ProjectName), HttpContext.Session.GetString(SessionConstants.Title) ?? "");
    }

    [HttpPost]
    public IActionResult SaveProjectName(string projectName)
    {
        HttpContext.Session.SetString(SessionConstants.Title, projectName ?? "");
        return RedirectToAction(nameof(Country));
    }

    public IActionResult Country()
    {
        return View(nameof(Country), HttpContext.Session.GetInt32(SessionConstants.Country));
    }

    [HttpPost]
    public IActionResult SaveCountry(string officeLocation)
    {
        HttpContext.Session.SetInt32(SessionConstants.Country, Int32.Parse(officeLocation));
        return RedirectToAction(nameof(ApplicationType));
    }

    public async Task<IActionResult> ApplicationType()
    {
        var categories = await categoriesService.GetApplicationCategories();
        ViewData[SessionConstants.ApplicationType] = HttpContext.Session.GetString(SessionConstants.ApplicationType)?.Split(",").ToList() ?? [];
        return View(categories);
    }

    [HttpPost]
    public IActionResult SaveApplicationType(string applicationType)
    {
        HttpContext.Session.SetString(SessionConstants.ApplicationType, applicationType ?? "");
        return RedirectToAction(nameof(ProjectCategory));
    }

    public async Task<IActionResult> ProjectCategory()
    {
        var categories = await categoriesService.GetProjectCategories();
        ViewData[SessionConstants.ProjectCategory] = HttpContext.Session.GetString(SessionConstants.ProjectCategory) ?? "";
        return View(categories);
    }

    [HttpPost]
    public IActionResult SaveProjectCategory(string projectCategory)
    {
        HttpContext.Session.SetString(SessionConstants.ProjectCategory, projectCategory ?? "");
        return RedirectToAction(nameof(ReviewAnswers));
    }

    public IActionResult ProjectStartDate()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SaveProjectStartDate(string projectStartDate)
    {
        ViewData[SessionConstants.StartDate] = projectStartDate;
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

        var id = HttpContext.Session.GetInt32(SessionConstants.Id);
        var application = GetApplicationFromSession();

        createdApplication = id == null ?
            await applicationsService.CreateApplication(application) :
            await applicationsService.UpdateApplication((int)id, application);

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
        return new IrasApplication
        {
            Title = HttpContext.Session.GetString(SessionConstants.Title),
            Location = (Location?)HttpContext.Session.GetInt32(SessionConstants.Country),
            ApplicationCategories = HttpContext.Session.GetString(SessionConstants.ApplicationType)?.Split(",").ToList() ?? [],
            ProjectCategory = HttpContext.Session.GetString(SessionConstants.ProjectCategory) ?? "",
            StartDate = DateTime.Parse(HttpContext.Session.GetString(SessionConstants.StartDate) ?? DateTime.Now.ToString()),
        };
    }
}