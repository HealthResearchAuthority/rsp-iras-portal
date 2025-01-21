using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[ExcludeFromCodeCoverage]
[Route("[controller]/[action]", Name = "questionset:[action]")]
[Authorize(Policy = "IsAdmin")]
public class QuestionSetController(IQuestionSetService questionSetService, IValidator<QuestionSetViewModel> validator) : Controller
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

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("Upload", "Please upload a file");
            await GetVersions(model);
            return View(nameof(Index), model);
        }

        var fileProcessResponse = questionSetService.ProcessQuestionSetFile(file);

        if (fileProcessResponse.Error != null)
        {
            ModelState.AddModelError("Upload", fileProcessResponse.ReasonPhrase!);
            await GetVersions(model);
            return View(nameof(Index), model);
        }

        if (!fileProcessResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError("Upload", "An unknown error occured");
            await GetVersions(model);
            return View(nameof(Index), model);
        }

        model.QuestionSetDto = fileProcessResponse.Content;

        if (!await ValidateQuestions(model))
        {
            await GetVersions(model);
            return View(nameof(Index), model);
        }

        var fileUploadResponse = await questionSetService.AddQuestionSet(fileProcessResponse.Content!);

        if (!fileUploadResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError("Upload", "Internal server error");
            await GetVersions(model);
            return View(nameof(Index), model);
        }

        TempData["UploadSuccess"] = true;

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
                ModelState.AddModelError("Upload", error.ErrorMessage);
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
            TempData["PublishSuccess"] = true;
            TempData["PublishedVersionId"] = versionId;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> PreviewApplication(string versionId, string categoryId = A)
    {
        var questions = default(List<QuestionViewModel>);

        if (questions == null || questions.Count == 0)
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

        return View(nameof(PreviewApplication), new QuestionnaireViewModel
        {
            CurrentStage = categoryId,
            Questions = questions
        });
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

        // store in temp data
        TempData[TempDataKeys.PreviousStage] = PreviousStage;
        TempData[TempDataKeys.CurrentStage] = CurrentStage;

        return (PreviousStage, CurrentStage, NextStage);
    }
}