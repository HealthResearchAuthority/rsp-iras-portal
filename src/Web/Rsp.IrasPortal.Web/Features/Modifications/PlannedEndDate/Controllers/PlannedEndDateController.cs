using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Application.Constants.TempDataKeys;

namespace Rsp.IrasPortal.Web.Features.Modifications.PlannedEndDate.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("modifications/plannedenddate/[action]", Name = "pmc:[action]")]
public class PlannedEndDateController
(
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<QuestionnaireViewModel> validator
) : ModificationChangesBaseController(respondentService, cmsQuestionsetService, validator)
{
    private readonly IRespondentService _respondentService = respondentService;
    private readonly ICmsQuestionsetService _cmsQuestionsetService = cmsQuestionsetService;
    private const string PostApprovalRoute = "pov:postapproval";

    /// <summary>
    /// Resumes the application for the categoryId
    /// </summary>
    /// <param name="projectRecordId">Application Id</param>
    /// <param name="categoryId">CategoryId to resume from</param>
    [HttpGet("/modifications/plannedenddate", Name = "pmc:[action]")]
    public async Task<IActionResult> PlannedEndDate(string projectRecordId, string categoryId, string sectionId, bool reviewAnswers = false)
    {
        return await DisplayQuestionnaire(projectRecordId, categoryId, sectionId, reviewAnswers, nameof(PlannedEndDate));
    }

    public async Task<IActionResult> AffectingOrganisations(string projectRecordId, string categoryId, string sectionId, bool reviewAnswers = false)
    {
        return await DisplayQuestionnaire(projectRecordId, categoryId, sectionId, reviewAnswers, nameof(AffectingOrganisations));
    }

    public async Task<IActionResult> AffectedOrganisationsType(string projectRecordId, string categoryId, string sectionId, bool reviewAnswers = false)
    {
        return await DisplayQuestionnaire(projectRecordId, categoryId, sectionId, reviewAnswers, nameof(AffectedOrganisationsType));
    }

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SaveResponses(QuestionnaireViewModel model, bool saveForLater = false)
    {
        var (projectModificationId, projectModificationChangeId) = CheckModification();

        var modificationQuestionnaire = TempData.Peek($"{TempDataKeys.ProjectModification.Questionnaire}:{model.CurrentStage}") as string;

        // get the questionnaire from the session
        // and deserialize it
        var questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(modificationQuestionnaire!)!;

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

        // At this point only validating the data format like date, email, length etc if provided
        // so that users can continue without entering the information. From the review screen
        // all mandatory questions will be validated
        var isValid = await ValidateQuestionnaire(model);

        // set the previous, current and next stages
        var navigation = await SetStage(model.CurrentStage!, questions);

        if (!isValid)
        {
            model.ReviewAnswers = false;
            return View(navigation.CurrentSection.StaticViewName, model);
        }

        // ------------------Save Modification Answers-------------------------
        // get the application from the session
        // to get the projectApplicationId
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;

        await SaveModificationAnswers(projectModificationChangeId, projectRecordId!, questions);

        // save the questions in the session
        TempData[$"{TempDataKeys.ProjectModification.Questionnaire}:{navigation.CurrentStage}"] = JsonSerializer.Serialize(questions);

        // if save for later
        if (saveForLater)
        {
            return RedirectToRoute(PostApprovalRoute, new { projectRecordId });
        }

        var isReviewInProgress = TempData.Peek(ProjectModificationChange.ReviewChanges) is true;

        if (navigation.CurrentSection?.IsMandatory is true)
        {
            var answers = questions.FindAll(question => !question.IsMissingAnswer());

            // if all the answers are missing redirect to Review page,
            // current section is mandatory for the next section
            if (answers.Count == 0)
            {
                navigation.PreviousStage = navigation.CurrentStage;
                navigation.PreviousCategory = navigation.CurrentCategory;
                navigation.PreviousSection = navigation.CurrentSection;
                navigation.NextCategory = string.Empty;
                navigation.NextStage = string.Empty;
                navigation.NextSection = null;

                TempData[ProjectModificationChange.Navigation] = JsonSerializer.Serialize(navigation);

                return RedirectToAction(nameof(ReviewChanges), new { projectRecordId });
            }

            // otherwise resume from the NextStage in sequence
            return RedirectToRoute($"pmc:{navigation.NextSection.StaticViewName}", new
            {
                projectRecordId,
                categoryId = navigation.NextCategory,
                sectionId = navigation.NextStage,
                reviewAnswers = isReviewInProgress
            });
        }

        // if review in progress or the user is at the last stage and clicks on Save and Continue
        if (isReviewInProgress || string.IsNullOrWhiteSpace(navigation.NextStage))
        {
            return RedirectToAction(nameof(ReviewChanges), new { projectRecordId });
        }

        // otherwise resume from the NextStage in sequence
        return RedirectToRoute($"pmc:{navigation.NextSection.StaticViewName}", new
        {
            projectRecordId,
            categoryId = navigation.NextCategory,
            sectionId = navigation.NextStage
        });
    }

    public async Task<IActionResult> DisplayQuestionnaire(string projectRecordId, string categoryId, string sectionId, bool reviewAnswers, string viewName)
    {
        // check if we are in the modification journey, so only get the modfication questions
        var (_, projectModificationChangeId) = CheckModification();

        if (projectModificationChangeId == Guid.Empty)
        {
            return this.ServiceError(new ServiceResponse
            {
                Error = "Modification change not found",
                ReasonPhrase = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                StatusCode = HttpStatusCode.BadRequest
            });
        }

        // get the responent answers for the category
        var respondentServiceResponse = await _respondentService.GetModificationAnswers(projectModificationChangeId, projectRecordId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        var sectionIdOrDefault = sectionId ?? string.Empty;

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
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsSetServiceResponse.Content!);

        var questions = questionnaire.Questions;

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            UpdateWithAnswers(respondentAnswers, questions);
        }

        TempData[ProjectModificationChange.ReviewChanges] = reviewAnswers;

        // save the list of QuestionViewModel in session to get it later
        TempData[$"{TempDataKeys.ProjectModification.Questionnaire}:{sectionId}"] = JsonSerializer.Serialize(questions);

        // this is where the questionnaire will resume
        var navigationDto = await SetStage(sectionIdOrDefault, questions);

        questionnaire.CurrentStage = navigationDto.CurrentStage;

        var viewModel = new QuestionnaireViewModel
        {
            CurrentStage = sectionId,
            Questions = questions
        };

        // if we have questions in the session
        // then return the view with the model
        return View(viewName, viewModel);
    }
}