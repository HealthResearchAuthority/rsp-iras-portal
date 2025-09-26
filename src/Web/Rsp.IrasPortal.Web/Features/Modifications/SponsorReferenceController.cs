using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly IRespondentService _respondentService = respondentService;
    private readonly ICmsQuestionsetService _cmsQuestionsetService = cmsQuestionsetService;
    private const string PostApprovalRoute = "pov:postapproval";
    private const string SectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";

    [HttpGet]
    public async Task<IActionResult> SponsorReference(string projectRecordId)
    {
        return await DisplayQuestionnaire(projectRecordId, CategoryId, SectionId);
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

        // get the responent answers for the category
        var respondentServiceResponse = await _respondentService.GetModificationAnswers(projectModificationId, projectRecordId, CategoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        var questionsSetServiceResponse = await _cmsQuestionsetService.GetModificationQuestionSet(SectionId);

        // return error page if unsuccessfull
        if (!questionsSetServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(questionsSetServiceResponse);
        }

        // get the respondent answers and questions
        var respondentAnswers = respondentServiceResponse.Content!;

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

        var projectRecordId = TempData.Peek(ProjectRecordId) as string;

        if (!saveForLater)
        {
            var validationResult = await validator.ValidateAsync(new ValidationContext<QuestionnaireViewModel>(model));

            if (!validationResult.IsValid)
            {
                // Add validation errors to the ModelState
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                // Return the view with validation errors
                return View(nameof(SponsorReference), model);
            }
        }

        await _respondentService.SaveModificationAnswers();

        // save service implementation
        if (saveForLater)
        {
            return RedirectToRoute(PostApprovalRoute, new { projectRecordId });
        }

        return RedirectToRoute("pmc:reviewallchanges");
    }

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
        var respondentServiceResponse = await _respondentService.GetModificationAnswers(projectModificationId, projectRecordId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        var questionsSetServiceResponse = await _cmsQuestionsetService.GetModificationQuestionSet(sectionId);

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
}