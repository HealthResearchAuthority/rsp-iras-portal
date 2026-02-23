using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.Portal.Application.Constants;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;
using static Rsp.Portal.Application.Constants.TempDataKeys;

namespace Rsp.Portal.Web.Features.Modifications;

[Authorize(Policy = Workspaces.MyResearch)]
[Route("/modifications/[action]", Name = "pmc:[action]")]
public class SponsorReferenceController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<QuestionnaireViewModel> validator,
    IFeatureManager featureManager
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator, featureManager)
{
    private readonly IRespondentService _respondentService = respondentService;
    private const string PostApprovalRoute = "pov:postapproval";
    private const string SectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
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

        var viewModel = await this.BuildSponsorQuestionnaireViewModel(projectModificationId, projectRecordId, categoryId);

        // if we have questions in the session
        // then return the view with the model
        return View(nameof(SponsorReference), viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
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

        if (!saveForLater)
        {
            var isValid = await this.ValidateQuestionnaire(validator, model) && await this.ValidateQuestionnaire(validator, model, true);

            if (!isValid)
            {
                return View(nameof(SponsorReference), model);
            }
        }

        // ------------------Save Modification Answers-------------------------
        var projectRecordId = TempData.Peek(ProjectRecordId) as string;
        var irasId = TempData.Peek(IrasId) as string;
        var shortTitle = TempData.Peek(ShortProjectTitle) as string;
        var status = TempData.Peek(ProjectModification.ProjectModificationStatus) as string;
        var sponsorOrganisationUserId = TempData.Peek(TempDataKeys.RevisionSponsorOrganisationUserId);
        var rtsId = TempData.Peek(TempDataKeys.RevisionRtsId) as string;

        await SaveModificationAnswers(projectModificationId, projectRecordId!, model.Questions);

        // if save for later, redirect to postapprovals
        if (saveForLater)
        {
            TempData[ShowNotificationBanner] = true;
            TempData[ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();

            if (status is ModificationStatus.ReviseAndAuthorise)
            {
                return RedirectToRoute("sws:modifications", new { sponsorOrganisationUserId, rtsId });
            }

            return RedirectToRoute(PostApprovalRoute, new { projectRecordId });
        }

        if (status is ModificationStatus.ReviseAndAuthorise)
        {
            return RedirectToRoute("pmc:ModificationDetails", new
            {
                projectRecordId,
                irasId,
                shortTitle,
                projectModificationId,
                sponsorOrganisationUserId,
                rtsId
            });
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