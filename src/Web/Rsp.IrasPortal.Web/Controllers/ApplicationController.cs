using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
[Authorize(Policy = "IsUser")]
public class ApplicationController(ILogger<ApplicationController> logger, IApplicationsService applicationsService) : Controller
{
    [AllowAnonymous]
    [Route("/", Name = "app:welcome")]
    public IActionResult Welcome()
    {
        logger.LogMethodStarted();

        return View(nameof(Index));
    }

    public IActionResult StartNewApplication()
    {
        logger.LogMethodStarted();

        HttpContext.Session.RemoveAllSessionValues();

        return View("ApplicationInfo", ("", "", "create"));
    }

    public async Task<IActionResult> EditApplication(string applicationId)
    {
        logger.LogMethodStarted();

        // get the pending application by id
        var response = await applicationsService.GetApplication(applicationId);

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

        // return the view if successfull
        if (!response.IsSuccessStatusCode)
        {
            // if status is forbidden or not found
            // return the appropriate response otherwise
            // return the generic error page
            return result.StatusCode switch
            {
                StatusCodes.Status403Forbidden => Forbid(),
                StatusCodes.Status404NotFound => NotFound(),
                _ => View("Error", result.Value)
            };
        }

        var irasApplication = (result.Value as IrasApplicationResponse)!;

        HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(irasApplication));

        return View("ApplicationInfo", (irasApplication.Title, irasApplication.Description, "edit"));
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplication(string projectName, string projectDescription)
    {
        logger.LogMethodStarted();

        var respondent = new RespondentDto
        {
            RespondentId = (HttpContext.Items[ContextItems.RespondentId] as string)!,
            EmailAddress = (HttpContext.Items[ContextItems.Email] as string)!,
            FirstName = (HttpContext.Items[ContextItems.FirstName] as string)!,
            LastName = (HttpContext.Items[ContextItems.LastName] as string)!,
            Role = string.Join(',', User.Claims
                       .Where(claim => claim.Type == ClaimTypes.Role)
                       .Select(claim => claim.Value))
        };

        var name = $"{respondent.FirstName} {respondent.LastName}";

        var irasApplicationRequest = new IrasApplicationRequest
        {
            Title = projectName,
            Description = projectDescription,
            CreatedBy = name,
            UpdatedBy = name,
            StartDate = DateTime.Now,
            Respondent = respondent
        };

        var response = await applicationsService.CreateApplication(irasApplicationRequest);

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

        // return the view if successfull
        if (!response.IsSuccessStatusCode)
        {
            // if status is forbidden or not found
            // return the appropriate response otherwise
            // return the generic error page
            return result.StatusCode switch
            {
                StatusCodes.Status403Forbidden => Forbid(),
                StatusCodes.Status404NotFound => NotFound(),
                _ => result
            };
        }

        var irasApplication = (this.ServiceResult(response).Value as IrasApplicationResponse)!;

        // save in TempData to retreive again in DraftSaved
        //TempData.TryAdd("td:draft-application", irasApplication, true);

        HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(irasApplication));

        return View("NewApplication", irasApplication);
    }

    //public async Task<IActionResult> LoadExistingApplication(string applicationIdSelect)
    //{
    //    logger.LogMethodStarted();

    //    if (applicationIdSelect == null)
    //    {
    //        return RedirectToAction(nameof(Welcome));
    //    }

    //    var response = await applicationsService.GetApplication(applicationIdSelect);

    //    // convert the service response to ObjectResult
    //    var application = this.ServiceResult(response).Value;

    //    if (application != null)
    //    {
    //        HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(application));

    //        return RedirectToAction(nameof(ProjectName));
    //    }

    //    HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(new IrasApplicationResponse()));

    //    return RedirectToAction(nameof(Welcome));
    //}

    public IActionResult DocumentUpload()
    {
        logger.LogMethodStarted();

        TempData.TryGetValue<List<Document>>("td:uploaded-documents", out var documents, true);

        return View(documents);
    }

    [HttpPost]
    public IActionResult Upload(IFormFileCollection formFiles)
    {
        logger.LogMethodStarted();

        List<Document> documents = [];

        foreach (var file in formFiles)
        {
            documents.Add(new Document
            {
                Name = file.FileName,
                Size = file.Length,
                Type = Path.GetExtension(file.FileName)
            });
        }

        TempData.TryAdd("td:uploaded-documents", documents, true);

        return RedirectToAction(nameof(DocumentUpload));
    }

    [HttpGet]
    public async Task<IActionResult> MyApplications()
    {
        logger.LogMethodStarted(LogLevel.Information);

        HttpContext.Session.RemoveAllSessionValues();

        // get the pending applications
        var response = await applicationsService.GetApplications();

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            return View(result.Value);
        }

        // if status is forbidden or not found
        // return the appropriate response otherwise
        // return the generic error page
        return result.StatusCode switch
        {
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => View("Error", result.Value)
        };
    }

    [Route("{applicationId}", Name = "app:ViewApplication")]
    public async Task<IActionResult> ViewApplication(string applicationId)
    {
        logger.LogMethodStarted(LogLevel.Information);

        // if the ModelState is invalid, return the view
        // with the null model. The view shouldn't display any
        // data as model is null
        if (!ModelState.IsValid)
        {
            return View("ApplicationView");
        }

        // get the pending application by id
        var response = await applicationsService.GetApplication(applicationId);

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            return View("ApplicationView", result.Value);
        }

        // if status is forbidden or not found
        // return the appropriate response otherwise
        // return the generic error page
        return result.StatusCode switch
        {
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => View("Error", result.Value)
        };
    }

    public IActionResult ReviewAnswers()
    {
        logger.LogMethodStarted();

        return View(GetApplicationFromSession());
    }

    [HttpPost]
    public async Task<IActionResult> SaveApplication()
    {
        logger.LogMethodStarted();

        var firstName = (HttpContext.Items[ContextItems.FirstName] as string)!;
        var lastName = (HttpContext.Items[ContextItems.LastName] as string)!;

        var name = $"{firstName} {lastName}";

        var request = new IrasApplicationRequest();

        var application = GetApplicationFromSession();

        request.ApplicationId = application.ApplicationId;
        request.Title = application.Title;
        request.Description = application.Description;
        request.UpdatedBy = name;

        await applicationsService.UpdateApplication(request);

        return RedirectToAction(nameof(MyApplications));
    }

    [AllowAnonymous]
    public IActionResult ViewportTesting()
    {
        logger.LogMethodStarted();

        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogMethodStarted();

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private IrasApplicationResponse GetApplicationFromSession()
    {
        logger.LogMethodStarted();

        var application = HttpContext.Session.GetString(SessionConstants.Application);

        if (application != null)
        {
            return JsonSerializer.Deserialize<IrasApplicationResponse>(application)!;
        }

        return new IrasApplicationResponse();
    }
}