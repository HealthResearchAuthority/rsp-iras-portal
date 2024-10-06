using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "qnc:[action]")]
[Authorize(Policy = "IsUser")]
public class QuestionnaireController(ILogger<ApplicationController> logger, IApplicationsService applicationsService, IRespondentService respondentService, IQuestionSetService questionSetService, IValidator<QuestionnaireViewModel> validator) : Controller
{
    public async Task<IActionResult> Resume(string applicationId, string categoryId)
    {
        if (await LoadApplication(applicationId) == null)
        {
            return NotFound();
        }

        // get the responent answers for the category
        var respondentAnswersResponse = await respondentService.GetRespondentAnswers(applicationId, categoryId);

        // get the questions for the category
        var questionsResponse = await questionSetService.GetNextQuestions(categoryId);

        var respondentAnswerResult = this.ServiceResult(respondentAnswersResponse);
        var questionsResult = this.ServiceResult(questionsResponse);

        // return the view if successfull
        if (!respondentAnswersResponse.IsSuccessStatusCode)
        {
            // if status is forbidden or not found
            // return the appropriate response otherwise
            // return the generic error page
            return View("Error", respondentAnswerResult.Value);
        }

        // return the view if successfull
        if (!questionsResponse.IsSuccessStatusCode)
        {
            // if status is forbidden or not found
            // return the appropriate response otherwise
            // return the generic error page
            return View("Error", questionsResult.Value);
        }

        var respondentAnswers = (respondentAnswerResult.Value as IEnumerable<RespondentAnswerDto>)!;
        var questions = (questionsResult.Value as IEnumerable<QuestionsResponse>)!;
        var questionnaire = BuildQuestionnaireViewModel(questions);

        if (respondentAnswers.Any())
        {
            var questionAnswers = questionnaire.Questions;

            foreach (var respondentAnswer in respondentAnswers)
            {
                var question = questionAnswers.Find(q => q.QuestionId == respondentAnswer.QuestionId)!;

                if (question == null)
                {
                    continue;
                }

                question.SelectedOption = respondentAnswer.SelectedOption;

                if (respondentAnswer.OptionType == "Multiple")
                {
                    question.Answers.ForEach(ans =>
                    {
                        var answer = respondentAnswer.Answers.Find(ra => ans.AnswerId == ra);
                        if (answer != null)
                        {
                            ans.IsSelected = true;
                        }
                    });
                }

                question.AnswerText = respondentAnswer.AnswerText;
            }

            questionnaire.Questions = questionAnswers;
        }

        HttpContext.Session.SetString(SessionConstants.Questionnaire, JsonSerializer.Serialize(questionnaire.Questions));

        return RedirectToAction(nameof(DisplayQuestionnaire), new { categoryId });
    }

    public async Task<IActionResult> DisplayQuestionnaire(string categoryId = A)
    {
        logger.LogMethodStarted();

        var questions = default(List<QuestionViewModel>);

        if (HttpContext.Session.Keys.Contains(SessionConstants.Questionnaire))
        {
            var questionsJson = HttpContext.Session.GetString(SessionConstants.Questionnaire)!;

            questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(questionsJson)!;
        }

        if (questions == null || questions.Count == 0)
        {
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

                var questionnaire = BuildQuestionnaireViewModel(response.Content!);

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

        return View("Index", new QuestionnaireViewModel
        {
            CurrentStage = categoryId,
            Questions = questions
        });
    }

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SaveResponses(QuestionnaireViewModel model)
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

        var stage = SetStage(model.CurrentStage!);

        // save the responses
        var application = this.GetApplicationFromSession();
        var respondentId = (HttpContext.Items[ContextItems.RespondentId] as string)!;

        var request = new RespondentAnswersRequest
        {
            ApplicationId = application.ApplicationId,
            RespondentId = respondentId
        };

        foreach (var question in questions)
        {
            if (string.IsNullOrWhiteSpace(question.AnswerText) &&
                !question.Answers.Exists(ans => ans.IsSelected) &&
                string.IsNullOrWhiteSpace(question.SelectedOption))
            {
                continue;
            }

            var optionType = question.DataType switch
            {
                "Boolean" or "Radio button" => "Single",
                "Checkbox" => "Multiple",
                _ => null
            };

            request.RespondentAnswers.Add(new RespondentAnswerDto
            {
                QuestionId = question.QuestionId,
                AnswerText = question.AnswerText,
                CategoryId = question.Category,
                SectionId = question.SectionId,
                SelectedOption = question.SelectedOption,
                OptionType = optionType,
                Answers = question.Answers
                                .Where(a => a.IsSelected)
                                .Select(ans => ans.AnswerId)
                                .ToList()
            });
        }

        if (request.RespondentAnswers.Count > 0)
        {
            await respondentService.SaveRespondentAnswers(request);
        }

        HttpContext.Session.SetString(SessionConstants.Questionnaire, JsonSerializer.Serialize(questions));

        return RedirectToAction(nameof(DisplayQuestionnaire), new { categoryId = stage.CurrentStage });
    }

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> Validate(QuestionnaireViewModel model)
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

            ViewData["vd:isvalid"] = false;
            // re-render the view when validation failed.
            return View("Index", model);
        }

        ViewData["vd:isvalid"] = true;

        return View("Index", model);

        //return RedirectToAction(nameof(DisplayQuestionnaire), new { categoryId = stage.NextStage });
    }

    private static QuestionnaireViewModel BuildQuestionnaireViewModel(IEnumerable<QuestionsResponse> response)
    {
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

    private async Task<IrasApplicationResponse?> LoadApplication(string applicationId)
    {
        // get the pending application by id
        var response = await applicationsService.GetApplication(applicationId);

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

        var irasApplication = (result.Value as IrasApplicationResponse);

        if (irasApplication != null)
        {
            HttpContext.Session.SetString(SessionConstants.Application, JsonSerializer.Serialize(irasApplication));
        }

        return irasApplication;
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