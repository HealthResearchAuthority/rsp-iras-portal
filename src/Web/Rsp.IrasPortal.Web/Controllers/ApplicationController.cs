using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
[Authorize(Policy = "IsUser")]
public class ApplicationController(IApplicationsService applicationsService,
                                   IValidator<ApplicationInfoViewModel> validator,
                                   IQuestionSetService questionSetService)
                                   : Controller
{
    // ApplicationInfo view name
    private const string ApplicationInfo = nameof(ApplicationInfo);

    [AllowAnonymous]
    [Route("/", Name = "app:welcome")]
    public IActionResult Welcome() => View(nameof(Index));

    public IActionResult CreateApplication() => View(nameof(CreateApplication));

    public async Task<IActionResult> StartProjectRecord()
    {
        var respondent = GetRespondentFromContext();

        var name = $"{respondent.FirstName} {respondent.LastName}";

        var irasApplicationRequest = new IrasApplicationRequest
        {
            Title = string.Empty,
            Description = string.Empty,
            CreatedBy = name,
            UpdatedBy = name,
            StartDate = DateTime.Now,
            Respondent = respondent
        };

        var applicationsServiceResponse = await applicationsService.CreateApplication(irasApplicationRequest);

        // return the view if successfull
        if (!applicationsServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(applicationsServiceResponse);
        }

        var irasApplication = applicationsServiceResponse.Content!;

        // save the application in session
        HttpContext.Session.SetString(SessionKeys.Application, JsonSerializer.Serialize(irasApplication));

        // get the questions for the category
        var questionSectionsResponse = await questionSetService.GetQuestionSections();
        var questionSections = questionSectionsResponse.Content;
        // Ensure questionSections is not null and has elements
        string categoryId = string.Empty;
        if (questionSections != null && questionSections.Any())
        {
            // get the first category
            categoryId = questionSections.First().QuestionCategoryId;
        }

        // continue to resume for the categoryId & applicationId
        return RedirectToAction("Resume", "Questionnaire", new
        {
            categoryId,
            irasApplication.ApplicationId
        });
    }

    public IActionResult StartNewApplication()
    {
        HttpContext.Session.RemoveAllSessionValues();

        return View(ApplicationInfo, (new ApplicationInfoViewModel(), "create"));
    }

    public async Task<IActionResult> EditApplication([FromQuery, Required] string applicationId)
    {
        // get the pending application by id
        var applicationsServiceResponse = await applicationsService.GetApplication(applicationId);

        // return the view if successfull
        if (!applicationsServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(applicationsServiceResponse);
        }

        var irasApplication = applicationsServiceResponse.Content!;

        HttpContext.Session.SetString(SessionKeys.Application, JsonSerializer.Serialize(irasApplication));

        var applicationInfo = new ApplicationInfoViewModel
        {
            Name = irasApplication.Title,
            Description = irasApplication.Description
        };

        return View(ApplicationInfo, (applicationInfo, "edit"));
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplication(ApplicationInfoViewModel model)
    {
        var context = new ValidationContext<ApplicationInfoViewModel>(model);

        var validationResult = await validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(ApplicationInfo, (model, "create"));
        }

        var respondent = GetRespondentFromContext();

        var name = $"{respondent.FirstName} {respondent.LastName}";

        var irasApplicationRequest = new IrasApplicationRequest
        {
            Title = model.Name!,
            Description = model.Description!,
            CreatedBy = name,
            UpdatedBy = name,
            StartDate = DateTime.Now,
            Respondent = respondent
        };

        var applicationsServiceResponse = await applicationsService.CreateApplication(irasApplicationRequest);

        // return the view if successfull
        if (!applicationsServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(applicationsServiceResponse);
        }

        var irasApplication = applicationsServiceResponse.Content!;

        // save the application in session
        HttpContext.Session.SetString(SessionKeys.Application, JsonSerializer.Serialize(irasApplication));

        return View("NewApplication", irasApplication);
    }

    public IActionResult DocumentUpload()
    {
        TempData.TryGetValue<List<Document>>(TempDataKeys.UploadedDocuments, out var documents, true);

        return View(documents);
    }

    [HttpPost]
    public IActionResult Upload(IFormFileCollection formFiles)
    {
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

        TempData.TryAdd(TempDataKeys.UploadedDocuments, documents, true);

        return RedirectToAction(nameof(DocumentUpload));
    }

    [HttpGet]
    [FeatureGate(Features.MyApplications)]
    public async Task<IActionResult> MyApplications()
    {
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        var avm = new ApplicationsViewModel();

        HttpContext.Session.RemoveAllSessionValues();

        // get the pending applications
        var applicationServiceResponse = await applicationsService.GetApplicationsByRespondent(respondentId);

        // return the view if successful
        if (applicationServiceResponse is { IsSuccessStatusCode: true, Content: not null })
        {
            avm.Applications = applicationServiceResponse.Content;

            var questionSetServiceResponse = await questionSetService.GetQuestionCategories();

            if (questionSetServiceResponse is { IsSuccessStatusCode: true, Content: not null })
            {
                avm.Categories = questionSetServiceResponse.Content;
                return View(avm);
            }

            // return the generic error page
            return this.ServiceError(questionSetServiceResponse);
        }

        // return the generic error page
        return this.ServiceError(applicationServiceResponse);
    }

    [Route("{applicationId}", Name = "app:ViewApplication")]
    public async Task<IActionResult> ViewApplication(string applicationId)
    {
        // if the ModelState is invalid, return the view
        // with the null model. The view shouldn't display any
        // data as model is null
        if (!ModelState.IsValid)
        {
            return View("ApplicationView");
        }

        // get the pending application by id
        var applicationServiceResponse = await applicationsService.GetApplication(applicationId);

        // return the view if successfull
        if (applicationServiceResponse.IsSuccessStatusCode)
        {
            return View("ApplicationView", applicationServiceResponse.Content);
        }

        // return the generic error page
        return this.ServiceError(applicationServiceResponse);
    }

    public IActionResult ReviewAnswers()
    {
        return View(this.GetApplicationFromSession());
    }

    [HttpPost]
    public async Task<IActionResult> SaveApplication(ApplicationInfoViewModel model)
    {
        var context = new ValidationContext<ApplicationInfoViewModel>(model);

        var validationResult = await validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(ApplicationInfo, (model, "edit"));
        }

        var respondent = new RespondentDto
        {
            RespondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
            EmailAddress = (HttpContext.Items[ContextItemKeys.Email] as string)!,
            FirstName = (HttpContext.Items[ContextItemKeys.FirstName] as string)!,
            LastName = (HttpContext.Items[ContextItemKeys.LastName] as string)!,
            Role = string.Join(',', User.Claims
                       .Where(claim => claim.Type == ClaimTypes.Role)
                       .Select(claim => claim.Value))
        };

        var name = $"{respondent.FirstName} {respondent.LastName}";

        var application = this.GetApplicationFromSession();

        var request = new IrasApplicationRequest
        {
            ApplicationId = application.ApplicationId,
            Title = model.Name!,
            Description = model.Description!,
            CreatedBy = application.CreatedBy,
            UpdatedBy = name,
            StartDate = application.CreatedDate,
            Respondent = respondent
        };

        await applicationsService.UpdateApplication(request);

        return RedirectToAction(nameof(MyApplications));
    }

    [AllowAnonymous]
    public IActionResult ViewportTesting()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private RespondentDto GetRespondentFromContext()
    {
        return new RespondentDto
        {
            RespondentId = HttpContext.Items[ContextItemKeys.RespondentId]?.ToString() ?? string.Empty,
            EmailAddress = HttpContext.Items[ContextItemKeys.Email]?.ToString() ?? string.Empty,
            FirstName = HttpContext.Items[ContextItemKeys.FirstName]?.ToString() ?? string.Empty,
            LastName = HttpContext.Items[ContextItemKeys.LastName]?.ToString() ?? string.Empty,
            Role = string.Join(',', User.Claims
                       .Where(claim => claim.Type == ClaimTypes.Role)
                       .Select(claim => claim.Value))
        };
    }
}