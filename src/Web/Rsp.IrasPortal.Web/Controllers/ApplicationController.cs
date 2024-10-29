using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasService.Application.DTOS.Requests;
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

        var response = await applicationsService.GetApplications();

        // convert the service response to ObjectResult
        var applications = this.ServiceResult(response);

        return View(nameof(Index), applications.Value);
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
            var response = model.Questions.Find(q => q.Index == question.Index);

            question.SelectedOption = response?.SelectedOption;
            question.Answers = response?.Answers ?? [];
            question.AnswerText = response?.AnswerText;
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

        var response = await applicationsService.GetApplication(applicationIdSelect);

        // convert the service response to ObjectResult
        var application = this.ServiceResult(response).Value;

        if (application != null)
        {
            HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(application));

            return RedirectToAction(nameof(ProjectName));
        }

        HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(new IrasApplicationResponse()));

        return RedirectToAction(nameof(Welcome));
    }

    public IActionResult ProjectName()
    {
        logger.LogMethodStarted();

        var application = HttpContext.Session.GetString(SessionConstants.Application)!;

        var irasApplication = JsonSerializer.Deserialize<IrasApplicationResponse>(application);

        return View(nameof(ProjectName), (irasApplication?.Title ?? "", irasApplication?.Description ?? ""));
    }

    [HttpPost]
    public IActionResult SaveProjectName(string projectName, string projectDescription)
    {
        logger.LogMethodStarted();

        var application = HttpContext.Session.GetString(SessionConstants.Application)!;

        var irasApplication = JsonSerializer.Deserialize<IrasApplicationResponse>(application)!;

        irasApplication.Title = projectName;
        irasApplication.Description = projectDescription;

        HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(irasApplication));

        return RedirectToAction(nameof(DocumentUpload));
    }

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
    public async Task<IActionResult> SaveApplication(string status)
    {
        logger.LogMethodStarted();

        var request = new IrasApplicationRequest();

        var application = GetApplicationFromSession();

        request.ApplicationId = application.ApplicationId;
        request.Status = status;
        request.Title = application.Title;
        request.Description = application.Description;
        request.StartDate = application.CreatedDate;
        request.CreatedBy = application.CreatedBy;
        request.UpdatedBy = application.UpdatedBy;

        await applicationsService.UpdateApplication(request);

        return RedirectToAction(nameof(MyApplications));
    }

    [HttpPost]
    public async Task<IActionResult> SaveDraftApplication(string status)
    {
        logger.LogMethodStarted();

        var request = new IrasApplicationRequest();

        var application = GetApplicationFromSession();

        request.ApplicationId = application.ApplicationId;
        request.Status = status;
        request.Title = application.Title;
        request.Description = application.Description;
        request.StartDate = application.CreatedDate;
        request.CreatedBy = application.CreatedBy;
        request.UpdatedBy = application.UpdatedBy;

        var response = request.ApplicationId == null ?
            await applicationsService.CreateApplication(request) :
            await applicationsService.UpdateApplication(request);

        var irasApplication = (this.ServiceResult(response).Value as IrasApplicationResponse)!;

        // save in TempData to retreive again in DraftSaved
        TempData.TryAdd("td:draft-application", irasApplication, true);

        return RedirectToAction(nameof(DraftSaved));
    }

    public IActionResult DraftSaved()
    {
        logger.LogMethodStarted();

        TempData.TryGetValue<IrasApplicationResponse>("td:draft-application", out var application, true);

        return View(application);
    }

    [AllowAnonymous]
    public IActionResult ViewportTesting()
    {
        logger.LogMethodStarted();

        return View();
    }

    [AllowAnonymous]
    public IActionResult QuestionSetUpload()
    {
        logger.LogMethodStarted();

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> QuestionSetUpload(QuestionSetFileModel model)
    {
        logger.LogMethodStarted();

        var file = model.Upload;

        //if (file == null || file.Length == 0)
        //{
        //    ModelState.AddModelError("Upload", "Please upload a file");
        //}

        //return View(model);

        if (file != null && file.Length > 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            List<DataTable> sheets = [
                result.Tables["A"],
                //result.Tables["B"],
                result.Tables["C1"],
                result.Tables["C4"],
                result.Tables["C6"],
                result.Tables["C7"],
                result.Tables["C8"]
            ];

            var projectFilterDataTable = result.Tables["A"]!;
            var rulesDataTable = result.Tables["App4 Rules"]!;
            var questionDtos = new List<QuestionDto>();

            foreach (var sheet in sheets)
            {
                foreach (DataRow question in sheet.Rows)
                {
                    var questionId = Convert.ToString(question[QuestionSetColumns.QuestionId]);

                    if (questionId == null || Convert.IsDBNull(questionId))
                    {
                        continue;
                    }

                    if (questionId.StartsWith("IQT"))
                    {
                        // handle section row
                        continue;
                    }

                    var conformance = Convert.ToString(question[QuestionSetColumns.Conformance]);

                    var questionDto = new QuestionDto
                    {
                        QuestionId = questionId!,
                        Category = Convert.ToString(question[QuestionSetColumns.Category])!,
                        SectionId = Convert.ToString(question[QuestionSetColumns.Section])!,
                        Section = Convert.ToString(question[QuestionSetColumns.Section])!,
                        Sequence = Convert.ToInt32(question[QuestionSetColumns.Sequence]),
                        Heading = Convert.ToString(question[QuestionSetColumns.Heading])!,
                        QuestionText = Convert.ToString(question[QuestionSetColumns.QuestionText])!,
                        QuestionType = Convert.ToString(question[QuestionSetColumns.QuestionType])!,
                        DataType = Convert.ToString(question[QuestionSetColumns.DataType])!,
                        IsMandatory = conformance == "Mandatory" || conformance == "Conditional mandatory",
                        IsOptional = conformance == "Optional",
                    };

                    var answers = Convert.ToString(question[QuestionSetColumns.Answers])!.Split(',');

                    //questionDto.Answers = answers.Select(answer => new AnswerDto
                    //{
                    //    AnswerId = answer,
                    //    AnswerText = answer,
                    //}).ToList();

                    questionDto.Answers = [];

                    //if (question[QuestionSetColumns.Rules] != null)
                    //{
                    //    var rulesDtos = new List<RuleDto>();
                    //    foreach (DataRow rule in rulesDataTable.Rows)
                    //    {
                    //        var ruleQuestionId = Convert.ToString(rule[RulesColumns.QuestionId])!;
                    //        if (ruleQuestionId != questionId) continue;

                    //        var ruleDto = new RuleDto
                    //        {
                    //            Sequence = Convert.ToInt32(rule[RulesColumns.Sequence]),
                    //            Operator = Convert.ToString(rule[RulesColumns.Operator])!,
                    //            QuestionId = Convert.ToString(rule[RulesColumns.QuestionId])!,
                    //            ParentQuestionId = Convert.ToString(rule[RulesColumns.ParentQuestionId])!,
                    //            Description = Convert.ToString(rule[RulesColumns.Description])!,
                    //        };

                    //        var conditionParentOptions = Convert.ToString(rule[RulesColumns.ConditionParentOptions])!.Split(',').ToList();

                    //        ruleDto.Condition = new ConditionDto
                    //        {
                    //            Comparison = Convert.ToString(rule[RulesColumns.ConditionComparison])!,
                    //            OptionsCountOperator = Convert.ToString(rule[RulesColumns.ConditionComparison])!,
                    //            ParentOptionsCount = conditionParentOptions.Count,
                    //            ParentOptions = conditionParentOptions,
                    //        };

                    //        rulesDtos.Add(ruleDto);
                    //    }

                    //    questionDto.Rules = rulesDtos;
                    //}

                    questionDto.Rules = [];

                    questionDtos.Add(questionDto);
                }
            }

            ViewBag.FileContent = questionDtos;

            var response = await questionSetService.CreateQuestions(questionDtos);
        }

        if (ModelState.IsValid)
        {
            ViewBag.Success = true;
        }

        return View(model);
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