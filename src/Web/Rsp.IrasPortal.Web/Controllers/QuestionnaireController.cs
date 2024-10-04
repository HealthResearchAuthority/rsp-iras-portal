using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "qnc:[action]")]
[Authorize(Policy = "IsUser")]
public class QuestionnaireController(ILogger<ApplicationController> logger, IQuestionSetService questionSetService, IValidator<QuestionnaireViewModel> validator) : Controller
{
    public async Task<IActionResult> DisplayQuestionnaire(string categoryId = A)
    {
        logger.LogMethodStarted();

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

            return View("Index", questionnaire);
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
            return View("Index", model);
        }

        return RedirectToAction(nameof(DisplayQuestionnaire), new { categoryId = stage.NextStage });
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