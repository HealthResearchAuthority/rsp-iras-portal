using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
[Authorize(Policy = "IsUser")]
public class ApplicationController(ILogger<ApplicationController> logger, IApplicationsService applicationsService, IQuestionSetService questionSetService, IValidator<ApplicationInfoViewModel> validator) : Controller
{
    // ApplicationInfo view name
    private const string ApplicationInfo = nameof(ApplicationInfo);

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

        return View(ApplicationInfo, (new ApplicationInfoViewModel(), "create"));
    }

    public async Task<IActionResult> EditApplication(string applicationId)
    {
        logger.LogMethodStarted();

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
        logger.LogMethodStarted();

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
        logger.LogMethodStarted();

        TempData.TryGetValue<List<Document>>(TempDataKeys.UploadedDocuments, out var documents, true);

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

        TempData.TryAdd(TempDataKeys.UploadedDocuments, documents, true);

        return RedirectToAction(nameof(DocumentUpload));
    }

    [HttpGet]
    public async Task<IActionResult> MyApplications()
    {
        logger.LogMethodStarted(LogLevel.Information);

        HttpContext.Session.RemoveAllSessionValues();

        // get the pending applications
        var applicationServiceResponse = await applicationsService.GetApplications();

        // return the view if successfull
        if (applicationServiceResponse.IsSuccessStatusCode)
        {
            return View(applicationServiceResponse.Content);
        }

        // return the generic error page
        return this.ServiceError(applicationServiceResponse);
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
        logger.LogMethodStarted();

        return View(this.GetApplicationFromSession());
    }

    [HttpPost]
    public async Task<IActionResult> SaveApplication(ApplicationInfoViewModel model)
    {
        logger.LogMethodStarted();

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
}