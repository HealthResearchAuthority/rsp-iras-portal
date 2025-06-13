using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using static System.Collections.Specialized.BitVector32;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[ExcludeFromCodeCoverage]
[Route("[controller]/[action]", Name = "qsc:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
public class QuestionSetController(IQuestionSetService questionSetService, IValidator<QuestionSetViewModel> validator)
    : Controller
{
    public async Task<IActionResult> Index(QuestionSetViewModel model)
    {
        await GetVersions(model);

        return View(model);
    }

    private async Task GetVersions(QuestionSetViewModel model)
    {
        var response = await questionSetService.GetVersions();
        if (response.IsSuccessStatusCode)
        {
            model.Versions = response.Content?.OrderByDescending(x => x.CreatedAt).ToList() ?? [];
        }
    }

    [HttpPost]
    public async Task<IActionResult> Upload(QuestionSetViewModel model)
    {
        var file = model.Upload;
        await GetVersions(model);

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(Upload), "Please upload a file");
            return View(nameof(Index), model);
        }

        if (model.Versions.Any(v => v.VersionId
                .Equals(Path.GetFileNameWithoutExtension(file.FileName), StringComparison.CurrentCultureIgnoreCase)))
        {
            ModelState.AddModelError(nameof(Upload), "Version name already exists");
            return View(nameof(Index), model);
        }

        var fileProcessResponse = questionSetService.ProcessQuestionSetFile(file);

        if (fileProcessResponse.Error != null)
        {
            ModelState.AddModelError(nameof(Upload), fileProcessResponse.ReasonPhrase!);
            return View(nameof(Index), model);
        }

        if (!fileProcessResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(nameof(Upload), "An unknown error occured");
            return View(nameof(Index), model);
        }

        model.QuestionSetDto = fileProcessResponse.Content;

        if (!await ValidateQuestions(model))
        {
            return View(nameof(Index), model);
        }

        var fileUploadResponse = await questionSetService.AddQuestionSet(fileProcessResponse.Content!);

        if (!fileUploadResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(nameof(Upload), "Internal server error");
            return View(nameof(Index), model);
        }

        TempData[TempDataKeys.QuestionSetUploadSuccess] = true;

        await GetVersions(model);
        return View(nameof(Index), model);
    }

    private async Task<bool> ValidateQuestions(QuestionSetViewModel model)
    {
        var context = new ValidationContext<QuestionSetViewModel>(model);

        context.RootContextData["questionDtos"] = model.QuestionSetDto!.Questions;

        var result = await validator.ValidateAsync(context);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(nameof(Upload), error.ErrorMessage);
            }

            return false;
        }

        return true;
    }

    [HttpPost]
    public async Task<IActionResult> PublishVersion(string versionId)
    {
        var response = await questionSetService.PublishVersion(versionId);

        if (response.IsSuccessStatusCode)
        {
            TempData[TempDataKeys.QuestionSetPublishSuccess] = true;
            TempData[TempDataKeys.QuestionSetPublishedVersionId] = versionId;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> PreviewApplication(string versionId, string categoryId = A)
    {
        // get the questions for the category
        var response = await questionSetService.GetQuestionsByVersion(versionId, categoryId);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            TempData.TryAdd(TempDataKeys.VersionId, versionId);

            // set the active stage for the category
            SetStage(categoryId);

            var questionnaire = BuildQuestionnaireViewModel(response.Content!);

            // store the questions to load again if there are validation errors on the page
            HttpContext.Session.SetString(SessionKeys.Questionnaire, JsonSerializer.Serialize(questionnaire.Questions));

            return View(nameof(PreviewApplication), questionnaire);
        }

        // return error page as api wasn't successful
        return this.ServiceError(response);
    }

    // GET: Start form from scratch
    [HttpGet]
    public IActionResult Create()
    {
        var model = new QuestionSetDto
        {
            Version = new VersionDto()
        };

        ViewBag.Step = "version";
        return View("QuestionSetForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVersion(QuestionSetDto model)
    {
        var versionId = model.Version?.VersionId;

        // If version ID is empty, also show a validation message
        if (string.IsNullOrWhiteSpace(versionId))
        {
            ModelState.AddModelError("Version.VersionId", "Enter a version ID");
            ViewBag.Step = "version";
            return View("QuestionSetForm", model);
        }

        // Get existing versions
        var response = await questionSetService.GetVersions();
        if (response.IsSuccessStatusCode)
        {
            var versions = response.Content?.OrderByDescending(x => x.CreatedAt).ToList() ?? [];

            // Check if the entered version ID already exists
            if (versions.Any(v => v.VersionId.Equals(versionId, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Version.VersionId", "A question set with this version ID already exists.");
                ViewBag.Step = "version";
                ViewBag.LockVersionId = false;
                return View("QuestionSetForm", model);
            }
        }

        model.Version.CreatedAt = DateTime.UtcNow;
        model.Version.IsDraft = false;
        model.Version.IsPublished = false;
        ViewBag.LockVersionId = true;

        // Save version separately in session
        HttpContext.Session.SetString($"questionset:{versionId}:version", JsonSerializer.Serialize(model.Version));
        HttpContext.Session.SetString($"questionset:{versionId}:questions",
            JsonSerializer.Serialize(new List<QuestionDto>()));

        ViewBag.Step = "questions";
        ViewBag.QuestionIndex = 0;

        return View("QuestionSetForm", model with { Questions = [] });
    }

    [HttpPost]
    public IActionResult AddQuestion(string versionId, QuestionDto question, int questionIndex)
    {
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");

        var version = string.IsNullOrWhiteSpace(versionJson)
            ? new VersionDto { VersionId = versionId }
            : JsonSerializer.Deserialize<VersionDto>(versionJson!)!;

        var questions = string.IsNullOrWhiteSpace(questionsJson)
            ? new List<QuestionDto>()
            : JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!;

        // MODAL VALIDATION 
        if (string.IsNullOrWhiteSpace(question.QuestionId))
        {
            ModelState.AddModelError("QuestionId", "Enter a question ID");
        }
        else
        {
            // Check for duplicate QuestionId, ignoring the current editing index
            var isDuplicate = questions
                .Where((q, index) => index != questionIndex) // exclude current question if editing
                .Any(q => string.Equals(q.QuestionId?.Trim(), question.QuestionId.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                ModelState.AddModelError("QuestionId", "This question ID has already been used");
            }
        }

        if (string.IsNullOrWhiteSpace(question.SectionId))
        {
            ModelState.AddModelError("SectionId", "Enter a section ID");
        }

        if (string.IsNullOrWhiteSpace(question.Section))
        {
            ModelState.AddModelError("Section", "Enter a section name");
        }

        if (question.Sequence <= 0)
        {
            ModelState.AddModelError("Sequence", "Enter a sequence number greater than 0");
        }
        else
        {
            // Check for duplicate Sequence within the same section, excluding the current question if editing
            bool isSequenceDuplicate = questions
                .Where((q, index) => index != questionIndex && string.Equals(q.Section?.Trim(),
                    question.Section?.Trim(), StringComparison.OrdinalIgnoreCase))
                .Any(q => q.Sequence == question.Sequence);

            if (isSequenceDuplicate)
            {
                ModelState.AddModelError("Sequence",
                    $"Sequence {question.Sequence} is already used in section \"{question.Section}\"");
            }
        }

        if (string.IsNullOrWhiteSpace(question.QuestionText))
        {
            ModelState.AddModelError("QuestionText", "Enter the question text");
        }

        if (string.IsNullOrWhiteSpace(question.QuestionType))
        {
            ModelState.AddModelError("QuestionType", "Select a question type");
        }

        // SORT DATA TYPE DEPENDING ON QUESTION TYPE
        if (question.QuestionType?.ToLowerInvariant() is "text" or "look-up list")
        {
            if (string.IsNullOrWhiteSpace(question.DataType))
            {
                ModelState.AddModelError("DataType", "Select a data type");
            }
        }
        else if (question.QuestionType?.ToLowerInvariant() is "date" or "boolean")
        {
            question.DataType = question.QuestionType.ToLower();
        }
        else if (question.QuestionType?.ToLowerInvariant() is "rts:org_lookup")
        {
            question.DataType = "text";
        }

        // CATEGORY NEEDS TO BE SET TO THE VERSION ID
        if (string.IsNullOrWhiteSpace(question.Category))
        {
            question.Category = "categoryid" + versionId;
        }

        // NEED TO SET ANSWERS HERE
        question.IsMandatory = true;
        question.IsOptional = false;

        if (question.DataType is "boolean")
        {
            var answers = new List<AnswerDto>
            {
                new AnswerDto()
                {
                    AnswerId = "Boolean1",
                    AnswerText = "Yes",
                    VersionId = versionId
                },
                new AnswerDto()
                {
                    AnswerId = "Boolean2",
                    AnswerText = "No",
                    VersionId = versionId
                }
            };

            question.Answers = answers;
        }
     
        // ON VALIDATION FAILURE 
        if (!ModelState.IsValid)
        {
            ViewBag.Step = "questions";
            ViewBag.QuestionIndex = questionIndex;
            ViewBag.UnsavedQuestion = question; // Store the unsaved input separately

            return View("QuestionSetForm", new QuestionSetDto
            {
                Version = version,
                Questions = questions
            });
        }

        // ON SUCCESS
        if (questionIndex >= 0 && questionIndex < questions.Count)
        {
            questions[questionIndex] = question;
        }
        else
        {
            questions.Add(question);
        }

        HttpContext.Session.SetString($"questionset:{versionId}:questions", JsonSerializer.Serialize(questions));

        ViewBag.Step = "questions";
        ViewBag.QuestionIndex = questions.Count; // always load next (empty) index

        return View("QuestionSetForm", new QuestionSetDto
        {
            Version = version,
            Questions = questions
        });
    }

    [HttpPost]
    public IActionResult EditQuestion(string versionId, int questionIndex)
    {
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");

        if (versionJson is null || questionsJson is null)
            return RedirectToAction("Create");

        var version = JsonSerializer.Deserialize<VersionDto>(versionJson)!;
        var questions = JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson)!;

        var model = new QuestionSetDto
        {
            Version = version,
            Questions = questions
        };

        ViewBag.Step = "questions";
        ViewBag.QuestionIndex = questionIndex;

        return View("QuestionSetForm", model);
    }


    [HttpPost]
    public IActionResult Back(string versionId, int questionIndex)
    {
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");

        var version = string.IsNullOrWhiteSpace(versionJson)
            ? new VersionDto { VersionId = versionId }
            : JsonSerializer.Deserialize<VersionDto>(versionJson!)!;

        var questions = string.IsNullOrWhiteSpace(questionsJson)
            ? new List<QuestionDto>()
            : JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!;

        if (questionIndex <= 0)
        {
            ViewBag.Step = "version";
            ViewBag.QuestionIndex = 0;
        }
        else
        {
            ViewBag.Step = "questions";
            ViewBag.QuestionIndex = questionIndex - 1;
        }

        ViewBag.LockVersionId = true;

        return View("QuestionSetForm", new QuestionSetDto
        {
            Version = version,
            Questions = questions
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(string versionId)
    {
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");
        var categoryList = new List<CategoryDto>()
        {
            new CategoryDto()
            {
                VersionId = versionId,
                CategoryId = "categoryid" + versionId,
                CategoryName = "categoryname" + versionId
            }
        };

        if (string.IsNullOrWhiteSpace(versionJson) || string.IsNullOrWhiteSpace(questionsJson))
            return RedirectToAction("Create");

        var model = new QuestionSetDto
        {
            Version = JsonSerializer.Deserialize<VersionDto>(versionJson!)!,
            Questions = JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!,
            Categories = categoryList
        };

        var addQuestionSet = await questionSetService.AddQuestionSet(model);

        if (!addQuestionSet.IsSuccessStatusCode)
        {
            ModelState.AddModelError(nameof(Save), "Internal server error");

            return View("QuestionSetForm", new QuestionSetDto
            {
                Version = JsonSerializer.Deserialize<VersionDto>(versionJson!)!,
                Questions = JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!
            });
        }

        // TODO: Save model to DB or service
        HttpContext.Session.Remove($"questionset:{versionId}:version");
        HttpContext.Session.Remove($"questionset:{versionId}:questions");

        ViewBag.Mode = "create";
        return View("SuccessMessage", model);
    }


    private static QuestionnaireViewModel BuildQuestionnaireViewModel(IEnumerable<QuestionsResponse> response)
    {
        // order the questions by SectionId and Sequence
        var questions = response
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

        return questionnaire;
    }

    private void SetStage(string category)
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

        // store in temp data
        TempData[TempDataKeys.PreviousStage] = PreviousStage;
        TempData[TempDataKeys.CurrentStage] = CurrentStage;
    }
}