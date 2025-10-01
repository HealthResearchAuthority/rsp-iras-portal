using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Application.Constants.TempDataKeys;

namespace Rsp.IrasPortal.Web.Features.Modifications;

[Route("/modifications/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsApplicant")]
public class SponsorReferenceController
(
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<QuestionnaireViewModel> validator
) : Controller
{
    private const string PostApprovalRoute = "pov:postapproval";
    private const string SectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";

    [HttpGet]
    public async Task<IActionResult> SponsorReference(string projectRecordId)
    {
        return await DisplayQuestionnaire(projectRecordId, CategoryId, SectionId);
    }

    [NonAction]
    public async Task<IActionResult> DisplayQuestionnaire(string projectRecordId, string categoryId, string sectionId)
    {
        // check if we are in the modification journey, so only get the modfication questions
        if (!Guid.TryParse(TempData.PeekGuid(ProjectModification.ProjectModificationId), out var projectModificationId) ||
            projectModificationId == Guid.Empty)
        {
            return this.ServiceError(new ServiceResponse
            {
                Error = "Modification not found",
                ReasonPhrase = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                StatusCode = HttpStatusCode.BadRequest
            });
        }

        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetModificationAnswers(projectModificationId, projectRecordId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        var questionsSetServiceResponse = await cmsQuestionsetService.GetModificationQuestionSet(sectionId);

        // return error page if unsuccessfull
        if (!questionsSetServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(questionsSetServiceResponse);
        }

        // get the respondent answers and questions
        var respondentAnswers = respondentServiceResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsSetServiceResponse.Content!, true);

        var questions = questionnaire.Questions;

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            ModificationHelpers.UpdateWithAnswers(respondentAnswers, questions);
        }

        var viewModel = new QuestionnaireViewModel
        {
            CurrentStage = sectionId,
            Questions = questions
        };

        // if we have questions in the session
        // then return the view with the model
        return View(nameof(SponsorReference), viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSponsorReference(QuestionnaireViewModel model, bool saveForLater = false)
    {
        // check if we are in the modification journey, so only get the modfication questions
        if (!Guid.TryParse(TempData.PeekGuid(ProjectModification.ProjectModificationId), out var projectModificationId) ||
            projectModificationId == Guid.Empty)
        {
            return this.ServiceError(new ServiceResponse
            {
                Error = "Modification not found",
                ReasonPhrase = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                StatusCode = HttpStatusCode.BadRequest
            });
        }

        var questionsSetServiceResponse = await cmsQuestionsetService.GetModificationQuestionSet(SectionId);

        // return error page if unsuccessfull
        if (!questionsSetServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(questionsSetServiceResponse);
        }

        // get the questionnaire from the session
        // and deserialize it
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsSetServiceResponse.Content!);

        var questions = questionnaire.Questions;

        // update the model with the answeres
        // provided by the applicant
        foreach (var question in questions)
        {
            // find the question in the submitted model
            // that matches the index
            var response = model.Questions.Find(q => q.Index == question.Index);

            // update the question with provided answers
            question.SelectedOption = response?.SelectedOption;

            if (question.DataType != "Dropdown")
            {
                question.Answers = response?.Answers ?? [];
            }

            question.AnswerText = response?.AnswerText;
            // update the date fields if they are present
            question.Day = response?.Day;
            question.Month = response?.Month;
            question.Year = response?.Year;
        }

        // override the submitted model
        // with the updated model with answers
        model.Questions = questions;

        var isValid = await ValidateQuestionnaire(model) && await ValidateQuestionnaire(model, true);

        if (!isValid)
        {
            return View(nameof(SponsorReference), model);
        }

        // ------------------Save Modification Answers-------------------------
        var projectRecordId = TempData.Peek(ProjectRecordId) as string;
        var irasId = TempData.Peek(IrasId) as string;
        var shortTitle = TempData.Peek(ShortProjectTitle) as string;

        await SaveModificationAnswers(projectModificationId, projectRecordId!, model.Questions);

        // if save for later, redirect to postapprovals
        if (saveForLater)
        {
            return RedirectToRoute(PostApprovalRoute, new { projectRecordId });
        }

        return RedirectToRoute("pmc:reviewallchanges", new
        {
            projectRecordId,
            irasId,
            shortTitle,
            projectModificationId
        });
    }

    private async Task SaveModificationAnswers(Guid projectModificationId, string projectRecordId, List<QuestionViewModel> questions)
    {
        // save the responses
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new ProjectModificationAnswersRequest
        {
            ProjectModificationId = projectModificationId,
            ProjectRecordId = projectRecordId,
            ProjectPersonnelId = respondentId
        };

        foreach (var question in questions)
        {
            // we need to identify if it's a
            // multiple choice or a single choice question
            // this is to determine if the responses
            // should be saved as comma seprated values
            // or a single value
            var optionType = question.DataType switch
            {
                "Boolean" or "Radio button" or "Look-up list" or "Dropdown" => "Single",
                "Checkbox" => "Multiple",
                _ => null
            };

            // build RespondentAnswers model
            request.ModificationAnswers.Add(new RespondentAnswerDto
            {
                QuestionId = question.QuestionId,
                VersionId = question.VersionId ?? string.Empty,
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

        // if user has answered some or all of the questions
        // call the api to save the responses
        if (request.ModificationAnswers.Count > 0)
        {
            await respondentService.SaveModificationAnswers(request);
        }
    }

    /// <summary>
    /// Validates the passed QuestionnaireViewModel and return ture or false
    /// </summary>
    /// <param name="model"><see cref="QuestionnaireViewModel"/> to validate</param>
    private async Task<bool> ValidateQuestionnaire(QuestionnaireViewModel model, bool validateMandatory = false)
    {
        // using the FluentValidation, create a new context for the model
        var context = new ValidationContext<QuestionnaireViewModel>(model);

        if (validateMandatory)
        {
            context.RootContextData["ValidateMandatoryOnly"] = true;
        }

        // this is required to get the questions in the validator
        // before the validation cicks in
        context.RootContextData["questions"] = model.Questions;

        // call the ValidateAsync to execute the validation
        // this will trigger the fluentvalidation using the injected validator if configured
        var result = await validator.ValidateAsync(context);

        if (!result.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return false;
        }

        return true;
    }
}