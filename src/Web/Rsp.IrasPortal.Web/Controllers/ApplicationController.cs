using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public class ApplicationController(ILogger<ApplicationController> logger, ICategoriesService categoriesService) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Welcome()
    {
        return RedirectToAction(nameof(Index));
    }

    public IActionResult ProjectName()
    {
        return View();
    }

    [HttpPost()]
    public IActionResult SaveProjectName(string projectName)
    {
        ViewData["ProjectName"] = projectName;
        return RedirectToAction(nameof(Country));
    }

    public IActionResult Country()
    {
        return View();
    }

    [HttpPost()]
    public IActionResult SaveCountry(string officeLocation)
    {
        ViewData["Country"] = officeLocation;
        return RedirectToAction(nameof(ApplicationType));
    }

    public async Task<IActionResult> ApplicationType()
    {
        var categories = await categoriesService.GetApplicationCategories();
        return View(categories);
    }

    [HttpPost()]
    public IActionResult SaveApplicationType(string applicationType)
    {
        ViewData["ApplicationType"] = applicationType;
        return RedirectToAction(nameof(ProjectCategory));
    }

    public async Task<IActionResult> ProjectCategory()
    {
        var categories = await categoriesService.GetProjectCategories();
        return View(categories);
    }

    [HttpPost()]
    public IActionResult SaveProjectCategory(string projectCategory)
    {
        ViewData["ProjectCategory"] = projectCategory;
        return RedirectToAction(nameof(ProjectStartDate));
    }

    public IActionResult ProjectStartDate()
    {
        return View();
    }

    [HttpPost()]
    public IActionResult SaveProjectStartDate(string projectStartDate)
    {
        ViewData["ProjectStartDate"] = projectStartDate;
        return RedirectToAction(nameof(DocumentUpload));
    }

    public IActionResult DocumentUpload()
    {
        return View();
    }

    [HttpPost()]
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