using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
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
using Rsp.Logging.Extensions;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
[Authorize(Policy = "IsUser")]
public class ApplicationController(ILogger<ApplicationController> logger, IValidator<QuestionnaireViewModel> validator, IApplicationsService applicationsService, IQuestionSetService questionSetService) : Controller
{
    [AllowAnonymous]
    public IActionResult SignIn()
    {
        logger.LogMethodStarted();

        return new ChallengeResult("OpenIdConnect", new()
        {
            RedirectUri = Url.Action(nameof(Welcome))
        });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Signout()
    {
        logger.LogMethodStarted();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync("OpenIdConnect");

        return new SignOutResult([CookieAuthenticationDefaults.AuthenticationScheme, "OpenIdConnect"], new()
        {
            RedirectUri = Url.Action(nameof(Welcome))
        });
    }

    [AllowAnonymous]
    [Route("/", Name = "app:welcome")]
    public async Task<IActionResult> Welcome()
    {
        logger.LogMethodStarted();

        var applications = await applicationsService.GetApplications();

        return View(nameof(Index), applications);
    }

    public IActionResult StartNewApplication()
    {
        logger.LogMethodStarted();

        HttpContext.Session.RemoveAllSessionValues();

        return RedirectToAction(nameof(DisplayQuestionnaire), new { categoryId = "A" });
    }

    public async Task<IActionResult> DisplayQuestionnaire(string categoryId = A)
    {
        logger.LogMethodStarted();

        HttpContext.Session.RemoveAllSessionValues();

        // get the initial questions for project filter if categoryId = A
        // otherwise get the questions for the other category
        var response = categoryId == A ?
            await questionSetService.GetInitialQuestions() :
            await questionSetService.GetNextQuestions(categoryId);

        logger.LogMethodStarted(LogLevel.Information);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            // set the active stage for the category
            SetStage(categoryId);

            var questions = response
                .Content!
                .OrderBy(q => q.SectionId)
                .ThenBy(q => q.Sequence)
                .Select((question, index) => (question, index));

            var questionnaire = new QuestionnaireViewModel();

            // build the questionnaire view model
            // we need to order the questions by section and sequence
            // and also need to assign the index to the question so the multiple choice
            // answsers can be linked back to the question
            foreach (var (question, index) in questions)
            {
                questionnaire.Questions.Add(new QuestionViewModel
                {
                    Index = index,
                    QuestionId = question.QuestionId,
                    Category = question.Category,
                    SectionId = question.SectionId,
                    Section = question.Section,
                    Sequence = question.Sequence,
                    Heading = question.Heading,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    DataType = question.DataType,
                    IsMandatory = question.IsMandatory,
                    IsOptional = question.IsOptional,
                    Rules = question.Rules,
                    Answers = question.Answers.Select(ans => new AnswerViewModel
                    {
                        AnswerId = ans.AnswerId,
                        AnswerText = ans.AnswerText
                    }).ToList()
                });
            }

            // store the questions to load again if there are validation errors on the page
            HttpContext.Session.SetString(SessionConstants.Questionnaire, JsonSerializer.Serialize(questionnaire.Questions));

            return View("Questionnaire", questionnaire);
        }

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

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

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SubmitAnswers(QuestionnaireViewModel model)
    {
        var questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(HttpContext.Session.GetString(SessionConstants.Questionnaire)!)!;

        foreach (var question in questions)
        {
            question.Answers = model.Questions.Find(q => q.Index == question.Index)?.Answers ?? [];
        }

        model.Questions = questions;

        var context = new ValidationContext<QuestionnaireViewModel>(model);

        context.RootContextData["questions"] = model.Questions;

        var result = await validator.ValidateAsync(context);

        var stage = SetStage(model.CurrentStage!);

        if (!result.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            // re-render the view when validation failed.
            return View("Questionnaire", model);
        }

        return RedirectToAction(nameof(DisplayQuestionnaire), new { categoryId = stage.NextStage });
    }

    public async Task<IActionResult> LoadExistingApplication(string applicationIdSelect)
    {
        logger.LogMethodStarted();

        if (applicationIdSelect == null)
        {
            return RedirectToAction(nameof(Welcome));
        }

        var applicationId = int.Parse(applicationIdSelect);
        var application = await applicationsService.GetApplication(applicationId);

        if (application != null)
        {
            HttpContext.Session.SetInt32(SessionConstants.Id, applicationId);
            HttpContext.Session.SetString(SessionConstants.Title, application.Title ?? "");
            HttpContext.Session.SetInt32(SessionConstants.Country, application.Location != null ? (int)application.Location : -1);
            HttpContext.Session.SetString(SessionConstants.ApplicationType, string.Join(",", application.ApplicationCategories ?? []));
            HttpContext.Session.SetString(SessionConstants.ProjectCategory, application.ProjectCategory ?? "");

            return RedirectToAction(nameof(ProjectName));
        }

        return RedirectToAction(nameof(Welcome));
    }

    public IActionResult ProjectName()
    {
        logger.LogMethodStarted();

        return View(nameof(ProjectName), HttpContext.Session.GetString(SessionConstants.Title) ?? "");
    }

    [HttpPost]
    public IActionResult SaveProjectName(string projectName)
    {
        logger.LogMethodStarted();

        HttpContext.Session.SetString(SessionConstants.Title, projectName ?? "");

        return RedirectToAction(nameof(Country));
    }

    public IActionResult Country()
    {
        logger.LogMethodStarted();

        return View(nameof(Country), HttpContext.Session.GetInt32(SessionConstants.Country));
    }

    [HttpPost]
    public IActionResult SaveCountry(string officeLocation)
    {
        logger.LogMethodStarted();

        HttpContext.Session.SetInt32(SessionConstants.Country, Int32.Parse(officeLocation));

        return RedirectToAction(nameof(ApplicationType));
    }

    public async Task<IActionResult> ApplicationType()
    {
        logger.LogMethodStarted();

        var categories = await applicationsService.GetApplicationCategories();

        ViewData[SessionConstants.ApplicationType] = HttpContext.Session.GetString(SessionConstants.ApplicationType)?.Split(",").ToList() ?? [];

        return View(categories);
    }

    [HttpPost]
    public IActionResult SaveApplicationType(string applicationType)
    {
        logger.LogMethodStarted();

        HttpContext.Session.SetString(SessionConstants.ApplicationType, applicationType ?? "");

        return RedirectToAction(nameof(ProjectCategory));
    }

    public async Task<IActionResult> ProjectCategory()
    {
        logger.LogMethodStarted();

        var categories = await applicationsService.GetProjectCategories();

        ViewData[SessionConstants.ProjectCategory] = HttpContext.Session.GetString(SessionConstants.ProjectCategory) ?? "";

        return View(categories);
    }

    [HttpPost]
    public IActionResult SaveProjectCategory(string projectCategory)
    {
        logger.LogMethodStarted();

        HttpContext.Session.SetString(SessionConstants.ProjectCategory, projectCategory ?? "");

        return RedirectToAction(nameof(ReviewAnswers));
    }

    public IActionResult ProjectStartDate()
    {
        logger.LogMethodStarted();

        return View();
    }

    [HttpPost]
    public IActionResult SaveProjectStartDate(string projectStartDate)
    {
        logger.LogMethodStarted();

        ViewData[SessionConstants.StartDate] = projectStartDate;

        return RedirectToAction(nameof(DocumentUpload));
    }

    public IActionResult DocumentUpload()
    {
        logger.LogMethodStarted();

        return View();
    }

    [HttpPost]
    public IActionResult SaveDocumentUpload(string supportingDocument)
    {
        logger.LogMethodStarted();

        ViewData["SupportingDocument"] = supportingDocument;

        return RedirectToAction(nameof(ReviewAnswers));
    }

    public IActionResult ReviewAnswers()
    {
        logger.LogMethodStarted();

        return View(GetApplicationFromSession());
    }

    [HttpPost]
    public async Task<IActionResult> SaveDraftApplication()
    {
        logger.LogMethodStarted();

        IrasApplication createdApplication;

        var id = HttpContext.Session.GetInt32(SessionConstants.Id);

        var application = GetApplicationFromSession();

        createdApplication = id == null ?
            await applicationsService.CreateApplication(application) :
            await applicationsService.UpdateApplication(id.Value, application);

        // save in TempData to retreive again in DraftSaved
        TempData.TryAdd("td:draft-application", createdApplication, true);

        return RedirectToAction(nameof(DraftSaved));
    }

    public IActionResult DraftSaved()
    {
        logger.LogMethodStarted();

        TempData.TryGetValue<IrasApplication>("td:draft-application", out var application, true);

        return View(application);
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

    private IrasApplication GetApplicationFromSession()
    {
        logger.LogMethodStarted();

        return new IrasApplication
        {
            Title = HttpContext.Session.GetString(SessionConstants.Title),
            Location = (Location?)HttpContext.Session.GetInt32(SessionConstants.Country),
            ApplicationCategories = HttpContext.Session.GetString(SessionConstants.ApplicationType)?.Split(",").ToList() ?? [],
            ProjectCategory = HttpContext.Session.GetString(SessionConstants.ProjectCategory) ?? "",
            StartDate = DateTime.Parse(HttpContext.Session.GetString(SessionConstants.StartDate) ?? DateTime.Now.ToString()),
        };
    }

    private (string PreviousStage, string CurrentStage, string NextStage) SetStage(string category)
    {
        (string? PreviousStage, string? CurrentStage, string NextStage) = category switch
        {
            A => ("", A, B),
            B => (A, B, C1),
            C1 => (B, C1, C2),
            C2 => (C1, C2, C3),
            C3 => (C2, C3, C4),
            C4 => (C3, C4, C5),
            C5 => (C4, C5, C6),
            C6 => (C5, C6, C7),
            C7 => (C6, C7, C8),
            C8 => (C7, C8, D),
            D => (C8, D, ""),
            _ => ("", A, B)
        };

        TempData["td:app_previousstage"] = PreviousStage;
        TempData["td:app_currentstage"] = CurrentStage;

        return (PreviousStage, CurrentStage, NextStage);
    }
}