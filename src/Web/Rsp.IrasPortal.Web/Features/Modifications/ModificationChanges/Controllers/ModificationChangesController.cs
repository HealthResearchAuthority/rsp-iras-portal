using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Application.Constants.TempDataKeys;

namespace Rsp.IrasPortal.Web.Features.Modifications.ModificationChanges.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("modifications/modificationchanges/[action]", Name = "pmc:[action]")]
public class ModificationChangesController
(
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<QuestionnaireViewModel> validator
) : ModificationChangesBaseController(respondentService, cmsQuestionsetService, validator)
{
    private readonly IRespondentService _respondentService = respondentService;
    private readonly ICmsQuestionsetService _cmsQuestionsetService = cmsQuestionsetService;
    private const string PostApprovalRoute = "pov:postapproval";

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SaveResponses(QuestionnaireViewModel model, bool saveForLater = false)
    {
        var (_, projectModificationChangeId) = CheckModification();

        var questionsSetServiceResponse = await _cmsQuestionsetService.GetModificationQuestionSet(model.CurrentStage);

        // return error page if unsuccessfull
        if (!questionsSetServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(questionsSetServiceResponse);
        }

        // get the questionnaire from the session
        // and deserialize it
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsSetServiceResponse.Content!);

        // update the model with the answeres
        // provided by the applicant
        questionnaire.UpdateWithAnswers(model.Questions);

        // override the submitted model
        // with the updated model with answers and rules
        model.Questions = questionnaire.Questions;

        // At this point only validating the data format like date, email, length etc if provided
        // so that users can continue without entering the information. From the review screen
        // all mandatory questions will be validated
        var isValid = await this.ValidateQuestionnaire(validator, model);

        // set the previous, current and next stages
        var sectionQuestions = questionnaire.Questions.FindAll(q => q.SectionId.Equals(model.CurrentStage!, StringComparison.OrdinalIgnoreCase));

        var question = sectionQuestions.FirstOrDefault(sq => sq.UseAnswerForNextSection);

        // this is where the questionnaire will resume
        var navigation = await SetStage(model.CurrentStage!, question?.QuestionId, question?.GetDisplayText());

        if (!isValid)
        {
            model.ReviewAnswers = false;
            return View("Questionnaire", model);
        }

        // ------------------Save Modification Answers-------------------------
        // get the application from the session
        // to get the projectApplicationId
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;

        await SaveModificationChangeAnswers(projectModificationChangeId, projectRecordId!, model.Questions);

        // if save for later
        if (saveForLater)
        {
            TempData[ShowNotificationBanner] = true;
            TempData[ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();

            return RedirectToRoute(PostApprovalRoute, new { projectRecordId });
        }

        var isReviewInProgress = TempData.Peek(ProjectModificationChange.ReviewChanges) is true;

        if (navigation.CurrentSection?.IsMandatory is true)
        {
            var answers = model.Questions.FindAll(question => !question.IsMissingAnswer());

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

                return RedirectToAction(nameof(ReviewChanges), new
                {
                    projectRecordId,
                    specificAreaOfChangeId = GetSpecificAreaOfChangeId(),
                    modificationChangeId = projectModificationChangeId
                });
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
        if (isReviewInProgress ||
            string.IsNullOrWhiteSpace(navigation.NextStage) ||
            navigation.NextSection.IsLastSectionBeforeReview ||
            navigation.NextSection.StaticViewName.Equals(nameof(ReviewChanges), StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(ReviewChanges), new
            {
                projectRecordId,
                specificAreaOfChangeId = GetSpecificAreaOfChangeId(),
                modificationChangeId = projectModificationChangeId
            });
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
        var respondentServiceResponse = await _respondentService.GetModificationChangeAnswers(projectModificationChangeId, projectRecordId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        var specificAreaOfChangeId = GetSpecificAreaOfChangeId();

        var questionsSetServiceResponse = await _cmsQuestionsetService.GetModificationsJourney(specificAreaOfChangeId.ToString());

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

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            questionnaire.UpdateWithRespondentAnswers(respondentAnswers);
        }

        TempData[ProjectModificationChange.ReviewChanges] = reviewAnswers;

        var sectionIdOrDefault = sectionId ?? string.Empty;

        // this is where the questionnaire will resume
        var navigationDto = await SetStage(sectionIdOrDefault);

        if (navigationDto.CurrentSection?.StoreUrlReferrer == true)
        {
            TempData[ProjectModification.UrlReferrer] = HttpContext.Request.GetDisplayUrl();
            TempData[ProjectModification.LinkBackToReferrer] = true;
        }

        questionnaire.CurrentStage = navigationDto.CurrentStage;

        var viewModel = new QuestionnaireViewModel
        {
            CurrentStage = sectionId,
            Questions = questionnaire.Questions
        };

        // see if the orginal answers are required to be shown
        await PopulateOriginalAnswers(projectRecordId, questionnaire.Questions, viewModel);

        // if we have questions in the session
        // then return the view with the model
        return View("Questionnaire", viewModel);
    }

    private async Task PopulateOriginalAnswers(string projectRecordId, List<QuestionViewModel> questions, QuestionnaireViewModel viewModel)
    {
        // if we need to show the original answer for the question
        var originalQuestions = questions.FindAll(q => q.ShowOriginalAnswer);

        if (originalQuestions.Count != 0)
        {
            // get the question set for the project record so that we can get the
            // original question text
            var questionSetResponse = await _cmsQuestionsetService.GetQuestionSet();

            var projectRecordQuestions = questionSetResponse.Content?.Sections.SelectMany(s => s.Questions) ?? [];

            // get the original respondent answers
            var projectAnswersResponse = await _respondentService.GetRespondentAnswers(projectRecordId);

            if (projectAnswersResponse.IsSuccessStatusCode)
            {
                var answers = projectAnswersResponse.Content;

                if (answers?.Any() is true)
                {
                    foreach (var originalQuestionId in originalQuestions.Select(originalQuestion => originalQuestion.QuestionId))
                    {
                        var projectRecordQuestion = projectRecordQuestions.SingleOrDefault(q => q.Id == originalQuestionId);

                        // get the answer for the question id
                        var originalAnswer = answers.FirstOrDefault(a => a.QuestionId == originalQuestionId);

                        if (originalAnswer != null)
                        {
                            // update the question text from the original question set
                            originalAnswer.QuestionText = projectRecordQuestion?.Name;

                            viewModel.ProjectRecordAnswers.Add(originalQuestionId, originalAnswer);
                        }
                    }
                }
            }
        }
    }
}