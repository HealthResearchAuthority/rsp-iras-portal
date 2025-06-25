using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[ExcludeFromCodeCoverage]
[Route("[controller]/[action]", Name = "qsc:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
public class QuestionSetController(IQuestionSetService questionSetService, IValidator<QuestionSetViewModel> validator)
    : Controller
{
    private const string StepAnswers = "answers";
    private const string StepQuestions = "questions";
    private const string StepVersion = "version";

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

    [HttpGet]
    [FeatureGate("QuestionSet.UseUI")]
    public IActionResult Create()
    {
        var model = new QuestionSetDto
        {
            Version = new VersionDto()
        };

        ViewData["Step"] = StepVersion;
        return View("QuestionSetForm", model);
    }

    [HttpPost]
    [FeatureGate("QuestionSet.UseUI")]
    public async Task<IActionResult> CreateVersion(QuestionSetDto model)
    {
        var versionId = model.Version?.VersionId;

        // If version ID is empty, also show a validation message
        if (string.IsNullOrWhiteSpace(versionId))
        {
            ModelState.AddModelError("Version.VersionId", "Enter a version ID");
            ViewData["Step"] = StepVersion;
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
                ViewData["Step"] = StepVersion;
                ViewBag.LockVersionId = false;
                return View("QuestionSetForm", model);
            }
        }

        if (model.Version != null)
        {
            model.Version.CreatedAt = DateTime.UtcNow;
            model.Version.IsDraft = true;
            model.Version.IsPublished = false;
            ViewData["LockVersionId"] = true;

            // Save version separately in session
            HttpContext.Session.SetString($"questionset:{versionId}:version", JsonSerializer.Serialize(model.Version));
        }

        HttpContext.Session.SetString($"questionset:{versionId}:questions",
            JsonSerializer.Serialize(new List<QuestionDto>()));

        ViewData["Step"] = StepQuestions;
        ViewData["QuestionIndex"] = 0;

        return View("QuestionSetForm", model with { Questions = [] });
    }

    [HttpPost]
    [FeatureGate("QuestionSet.UseUI")]
    public IActionResult AddQuestion(string versionId, QuestionDto question, int questionIndex)
    {
        var answersJson = HttpContext.Session.GetString($"questionset:{versionId}:{questionIndex}:questionanswers");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");

        var answers = string.IsNullOrWhiteSpace(answersJson)
            ? []
            : JsonSerializer.Deserialize<List<AnswerDto>>(answersJson!)!;
        var questions = string.IsNullOrWhiteSpace(questionsJson)
            ? []
            : JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!;

        while (questions.Count <= questionIndex)
            questions.Add(new QuestionDto { VersionId = versionId });

        question.Answers = answers;
        questions[questionIndex] = question;

        if (questionIndex >= questions.Count)
        {
            while (questions.Count <= questionIndex)
                questions.Add(new QuestionDto { VersionId = versionId });
        }

        if (string.IsNullOrWhiteSpace(question.QuestionId))
            question.QuestionId = $"{versionId}-{questionIndex}";

        // Validation
        if (string.IsNullOrWhiteSpace(question.Section))
        {
            ModelState.AddModelError("Section", "Enter a section name");
        }

        if (string.IsNullOrWhiteSpace(question.QuestionText))
        {
            ModelState.AddModelError("QuestionText", "Enter the question text");
        }

        if (string.IsNullOrWhiteSpace(question.QuestionType))
        {
            ModelState.AddModelError("QuestionType", "Select a question type");
        }

        if (question.QuestionType?.ToLowerInvariant() is "text" or "look-up list" &&
            string.IsNullOrWhiteSpace(question.DataType))
        {
            ModelState.AddModelError("DataType", "Select a data type");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Step"] = StepQuestions;
            ViewData["QuestionIndex"] = questionIndex;
            ViewData["UnsavedQuestion"] = question;

            var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");
            var version = string.IsNullOrWhiteSpace(versionJson)
                ? new VersionDto { VersionId = versionId }
                : JsonSerializer.Deserialize<VersionDto>(versionJson!)!;

            return View("QuestionSetForm", new QuestionSetDto
            {
                Version = version,
                Questions = questions
            });
        }

        questions[questionIndex] = question;
        HttpContext.Session.SetString($"questionset:{versionId}:questions", JsonSerializer.Serialize(questions));

        var versionFinal =
            JsonSerializer.Deserialize<VersionDto>(HttpContext.Session.GetString($"questionset:{versionId}:version")!);

        ViewData["Step"] = question.QuestionType?.ToLowerInvariant() == "look-up list" ? StepAnswers : StepQuestions;
        ViewData["QuestionIndex"] = question.QuestionType?.ToLowerInvariant() == "look-up list"
            ? questionIndex
            : questionIndex + 1;

        return View("QuestionSetForm", new QuestionSetDto
        {
            Version = versionFinal,
            Questions = questions
        });
    }

    [HttpPost]
    [FeatureGate("QuestionSet.UseUI")]
    public IActionResult EditQuestion(string versionId, int questionIndex)
    {
        var answersJson = HttpContext.Session.GetString($"questionset:{versionId}:{questionIndex}:questionanswers");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");

        var answers = string.IsNullOrWhiteSpace(answersJson)
            ? []
            : JsonSerializer.Deserialize<List<AnswerDto>>(answersJson!)!;
        var questions = string.IsNullOrWhiteSpace(questionsJson)
            ? []
            : JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!;
        var version = string.IsNullOrWhiteSpace(versionJson)
            ? new VersionDto { VersionId = versionId }
            : JsonSerializer.Deserialize<VersionDto>(versionJson!)!;

        var question = questions[questionIndex];
        question.Answers = answers;
        questions[questionIndex] = question;

        if (versionJson is null || questionsJson is null)
            return RedirectToAction("Create");

        var model = new QuestionSetDto
        {
            Version = version,
            Questions = questions
        };

        ViewData["Step"] = StepQuestions;
        ViewData["QuestionIndex"] = questionIndex;

        return View("QuestionSetForm", model);
    }

    [HttpPost]
    [FeatureGate("QuestionSet.UseUI")]
    public IActionResult AddAnswer(string versionId, int questionIndex, AnswerDto answer, string? originalAnswerId)
    {
        var answersJson = HttpContext.Session.GetString($"questionset:{versionId}:{questionIndex}:questionanswers");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");

        var answers = string.IsNullOrWhiteSpace(answersJson)
            ? []
            : JsonSerializer.Deserialize<List<AnswerDto>>(answersJson!)!;
        var questions = string.IsNullOrWhiteSpace(questionsJson)
            ? []
            : JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!;
        var version = string.IsNullOrWhiteSpace(versionJson)
            ? new VersionDto { VersionId = versionId }
            : JsonSerializer.Deserialize<VersionDto>(versionJson!)!;

        while (questions.Count <= questionIndex)
            questions.Add(new QuestionDto { VersionId = versionId });

        var question = questions[questionIndex];
        question.Answers = answers;

        if (string.IsNullOrWhiteSpace(answer.AnswerText) && !string.IsNullOrWhiteSpace(originalAnswerId))
        {
            var selected = answers.FirstOrDefault(a => a.AnswerId == originalAnswerId);

            ViewData["Step"] = StepAnswers;
            ViewData["QuestionIndex"] = questionIndex;
            ViewData["UnsavedQuestion"] = question;
            ViewData[$"SelectedAnswerId_{questionIndex}"] = selected?.AnswerId;
            ViewData["IsEditingAnswer"] = true;
            ViewData["OriginalAnswerId"] = originalAnswerId;

            return View("QuestionSetForm", new QuestionSetDto
            {
                Version = version,
                Questions = questions
            });
        }

        if (string.IsNullOrWhiteSpace(answer.AnswerText))
        {
            ModelState.AddModelError("answer.AnswerText", "Enter an answer text");
        }
        else
        {
            // Normalize and compare to check if answer already exists
            var existingAnswers = question.Answers ?? (List<AnswerDto>) [];
            bool duplicateExists = existingAnswers.Any(a =>
                    !string.IsNullOrWhiteSpace(a.AnswerText) &&
                    a.AnswerText.Trim().Equals(answer.AnswerText.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    a.AnswerId != answer.AnswerId // skip current one if editing
            );

            if (duplicateExists)
            {
                ModelState.AddModelError("answer.AnswerText", "An answer with this text already exists");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["Step"] = StepAnswers;
            ViewData["QuestionIndex"] = questionIndex;
            ViewData["UnsavedQuestion"] = question;
            ViewData[$"SelectedAnswerId_{questionIndex}"] = answer.AnswerId;
            ViewData["IsEditingAnswer"] = !string.IsNullOrWhiteSpace(originalAnswerId);
            ViewData["OriginalAnswerId"] = originalAnswerId;

            return View("QuestionSetForm", new QuestionSetDto
            {
                Version = version,
                Questions = questions
            });
        }

        if (string.IsNullOrWhiteSpace(answer.AnswerId))
        {
            answer.AnswerId = $"{versionId}-{questionIndex}-{answers.Count + 1}";
        }

        answer.VersionId = versionId;
        answers.Add(answer);

        question.Answers = answers;
        HttpContext.Session.SetString($"questionset:{versionId}:{questionIndex}:questionanswers",
            JsonSerializer.Serialize(answers));

        ViewData["Step"] = StepAnswers;
        ViewData["QuestionIndex"] = questionIndex;

        return View("QuestionSetForm", new QuestionSetDto
        {
            Version = version,
            Questions = questions
        });
    }

    [HttpPost]
    [FeatureGate("QuestionSet.UseUI")]
    public IActionResult CompleteQuestionAndAnswers(string versionId, int questionIndex)
    {
        var answersJson = HttpContext.Session.GetString($"questionset:{versionId}:{questionIndex}:questionanswers");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");

        var version = string.IsNullOrWhiteSpace(versionJson)
            ? new VersionDto { VersionId = versionId }
            : JsonSerializer.Deserialize<VersionDto>(versionJson!)!;
        var questions = string.IsNullOrWhiteSpace(questionsJson)
            ? []
            : JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!;
        var answers = string.IsNullOrWhiteSpace(answersJson)
            ? []
            : JsonSerializer.Deserialize<List<AnswerDto>>(answersJson!)!;

        var question = questions[questionIndex];

        question.Answers = answers;
        questions[questionIndex] = question;

        ViewData["Step"] = StepQuestions;
        ViewData["QuestionIndex"] = questionIndex + 1;

        HttpContext.Session.SetString($"questionset:{versionId}:questions", JsonSerializer.Serialize(questions));
        HttpContext.Session.SetString($"questionset:{versionId}:{questionIndex}:questionanswers",
            JsonSerializer.Serialize(answers));

        return View("QuestionSetForm", new QuestionSetDto
        {
            Version = version,
            Questions = questions
        });
    }

    [HttpPost]
    [FeatureGate("QuestionSet.UseUI")]
    public async Task<IActionResult> Save(string versionId)
    {
        var versionJson = HttpContext.Session.GetString($"questionset:{versionId}:version");
        var questionsJson = HttpContext.Session.GetString($"questionset:{versionId}:questions");

        var version = string.IsNullOrWhiteSpace(versionJson)
            ? new VersionDto { VersionId = versionId }
            : JsonSerializer.Deserialize<VersionDto>(versionJson!)!;
        var questions = string.IsNullOrWhiteSpace(questionsJson)
            ? []
            : JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson!)!;

        List<CategoryDto> categoryList =
        [
            new()
            {
                VersionId = versionId,
                CategoryId = "categoryid_" + versionId.ToLower(),
                CategoryName = "categoryname_" + versionId.ToLower(),
            }
        ];

        if (string.IsNullOrWhiteSpace(versionJson) || string.IsNullOrWhiteSpace(questionsJson))
            return RedirectToAction("Create");

        for (var i = 0; i < questions.Count; i++)
        {
            questions[i].Category = "categoryid_" + versionId.ToLower();
            questions[i].IsMandatory = true;
            questions[i].IsOptional = false;
            questions[i].SectionId = versionId.ToLower() + questions[i].Section.Replace(" ", "");

            if (questions[i].QuestionType?.ToLowerInvariant() is not ("text" or "look-up list" or "rts:org_lookup"))
            {
                // SET QUESTION TYPE TO DATATYPE
                questions[i].DataType = questions[i].QuestionType;

                if (questions[i].QuestionType?.ToLowerInvariant() is "boolean")
                {
                    questions[i].Answers = (List<AnswerDto>)
                    [
                        new AnswerDto
                        {
                            AnswerId = $"{versionId}-{i}-0",
                            AnswerText = "Yes",
                            VersionId = versionId,
                        },

                        new AnswerDto
                        {
                            AnswerId = $"{versionId}-{i}-1",
                            AnswerText = "No",
                            VersionId = versionId,
                        }

                    ];
                }
            }

            if (questions[i].QuestionType?.ToLowerInvariant() is "rts:org_lookup")
            {
                questions[i].DataType = "text";

                questions[i].Rules = (List<RuleDto>)
                [
                    new RuleDto
                    {
                        Description = "Please start typing to add your primary sponsor org.",
                        QuestionId = questions[i].QuestionId,
                        Mode = "AND",
                        Sequence = 1,
                        VersionId = versionId,
                        ParentQuestionId = null,

                        Conditions = (List<ConditionDto>)
                        [
                            new ConditionDto
                            {
                                Description = "Please start typing to add your primary sponsor org.",
                                Mode = "AND",
                                Negate = false,
                                Operator = "HINT",
                                Value = "15,100",
                                OptionType = questions[i].QuestionType,
                                IsApplicable = true,
                            }
                        ]
                    }
                ];
            }
        }

        var model = new QuestionSetDto
        {
            Version = version!,
            Questions = questions,
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