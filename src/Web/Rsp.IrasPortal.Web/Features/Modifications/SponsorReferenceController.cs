using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Application.Constants.TempDataKeys;

namespace Rsp.IrasPortal.Web.Features.Modifications;

[Authorize(Policy = Workspaces.MyResearch)]
[Route("/modifications/[action]", Name = "pmc:[action]")]
public class SponsorReferenceController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<QuestionnaireViewModel> validator
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator)
{
    private readonly IRespondentService _respondentService = respondentService;
    private const string PostApprovalRoute = "pov:postapproval";
    private const string SectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";

    [Authorize(Policy = Permissions.MyResearch.Modifications_Update)]
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
        var respondentServiceResponse = await _respondentService.GetModificationAnswers(projectModificationId, projectRecordId, categoryId);

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

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            questionnaire.UpdateWithRespondentAnswers(respondentAnswers);
        }

        var viewModel = new QuestionnaireViewModel
        {
            CurrentStage = sectionId,
            Questions = questionnaire.Questions
        };

        // if we have questions in the session
        // then return the view with the model
        return View(nameof(SponsorReference), viewModel);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Update)]
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

        // update the model with the answeres
        // provided by the applicant
        questionnaire.UpdateWithAnswers(model.Questions);

        // override the submitted model
        // with the updated model with answers and rules
        model.Questions = questionnaire.Questions;

        var isValid = await this.ValidateQuestionnaire(validator, model) && await this.ValidateQuestionnaire(validator, model, true);

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
            TempData[ShowNotificationBanner] = true;
            TempData[ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();

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
}