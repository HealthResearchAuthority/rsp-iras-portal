using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Authorize(Policy = Workspaces.MyResearch)]
[Route("modifications/participatingorganisations/[action]", Name = "pod:[action]")]
public class ParticipatingOrganisationDetailsController
(
    ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService,
    IValidator<QuestionnaireViewModel> validator
) : Controller
{
    private const string OrganisationDetailsSection = "pom-participating-organisation-details";
    private const string PostApprovalRoute = "pov:postapproval";
    private const string ReviewAllChangesRoute = "pmc:reviewallchanges";
    private const string ReviewChangesRoute = "pmc:reviewchanges";
    private const string OrganisationsListRoute = "pol:participatingorganisationslist";

    /// <summary>
    /// Returns the view for selecting participating organisations.
    /// Populates metadata from TempData.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    public async Task<IActionResult> ParticipatingOrganisationDetails(Guid organisationId, bool reviewAnswers = false, bool reviewAllChanges = false)
    {
        var baseModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        // Populate the view model with base project and document metadata.
        var viewModel = new OrganisationDetailsViewModel
        {
            ProjectRecordId = baseModel.ProjectRecordId,
            ShortTitle = baseModel.ShortTitle,
            IrasId = baseModel.IrasId,
            ModificationIdentifier = baseModel.ModificationIdentifier,
            ModificationId = baseModel.ModificationId,
            ReviewAnswers = reviewAnswers,
            ReviewAllChanges = reviewAllChanges,
        };

        // Fetch the CMS question set that defines what metadata must be collected for this document.
        var additionalQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(OrganisationDetailsSection);

        // Build the questionnaire model containing all questions for the details section.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Retrieve any existing answers the user may have already provided for this document.
        var answersResponse = await respondentService
            .GetModificationParticipatingOrganisationAnswers(organisationId);

        var answers = answersResponse?.StatusCode == HttpStatusCode.OK
            ? answersResponse.Content ?? []
            : [];

        var questionIndex = 0;

        questionnaire.Questions = questionnaire.Questions.ConvertAll(cmsQ =>
        {
            var matchingAnswer = answers.FirstOrDefault(a => a.QuestionId == cmsQ.QuestionId);

            return new QuestionViewModel
            {
                Id = matchingAnswer?.Id,
                Index = questionIndex++,
                QuestionId = cmsQ.QuestionId,
                VersionId = cmsQ.VersionId,
                Category = cmsQ.Category,
                SectionId = cmsQ.SectionId,
                Section = cmsQ.Section,
                Sequence = cmsQ.Sequence,
                Heading = cmsQ.Heading,
                QuestionText = cmsQ.QuestionText,
                QuestionType = cmsQ.QuestionType,
                DataType = cmsQ.DataType,
                IsMandatory = cmsQ.IsMandatory,
                IsOptional = cmsQ.IsOptional,
                AnswerText = matchingAnswer?.AnswerText,
                SelectedOption = matchingAnswer?.SelectedOption,
                Answers = cmsQ?.Answers?
                .Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.AnswerId,
                    AnswerText = ans.AnswerText,
                    // Set true if CMS already marked it OR if it exists in answersResponse
                    IsSelected = ans.IsSelected || answers.Any(a => a.SelectedOption == ans.AnswerId)
                })
                .ToList() ?? [],
                Rules = cmsQ?.Rules ?? [],
                ShortQuestionText = cmsQ?.ShortQuestionText ?? string.Empty,
                IsModificationQuestion = true,
                GuidanceComponents = cmsQ?.GuidanceComponents ?? []
            };
        });

        questionnaire.UpdateWithRespondentAnswers(answers);

        // Attach the populated questionnaire to the view model.
        viewModel.Questions = questionnaire.Questions;

        // Render the "Add Document Details" view for the selected document.
        return View(viewModel);
    }

    /// <summary>
    /// Returns the view for selecting participating organisations.
    /// Populates metadata from TempData.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpPost]
    public async Task<IActionResult> SaveOrganisationDetailsForReviewAll(OrganisationDetailsViewModel model)
    {
        bool validationResult = await ValidateModel(model);

        if (!validationResult)
        {
            return View(nameof(ParticipatingOrganisationDetails), model);
        }

        var baseModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        var participatingOrganisationAnswers = model.ToAnswersDto();

        var saveAnswersResponse = await respondentService
            .SaveModificationParticipatingOrganisationAnswers(participatingOrganisationAnswers);

        if (!saveAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveAnswersResponse);
        }

        return RedirectToRoute
        (
            ReviewAllChangesRoute,
            new
            {
                projectRecordId = baseModel.ProjectRecordId,
                irasId = baseModel.IrasId,
                shortTitle = baseModel.ShortTitle,
                projectModificationId = baseModel.ModificationId
            }
        );
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpPost]
    public async Task<IActionResult> SaveOrganisationDetailsForReview(OrganisationDetailsViewModel model)
    {
        bool validationResult = await ValidateModel(model);

        if (!validationResult)
        {
            return View(nameof(ParticipatingOrganisationDetails), model);
        }

        var baseModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        var participatingOrganisationAnswers = model.ToAnswersDto();

        var saveAnswersResponse = await respondentService
            .SaveModificationParticipatingOrganisationAnswers(participatingOrganisationAnswers);

        if (!saveAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveAnswersResponse);
        }

        return RedirectToRoute
        (
            ReviewChangesRoute,
            new
            {
                projectRecordId = baseModel.ProjectRecordId,
                specificAreaOfChangeId = baseModel.SpecificAreaOfChangeId,
                modificationChangeId = baseModel.ModificationChangeId,
                reviseChange = true
            }
        );
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpPost]
    public async Task<IActionResult> SaveOrganisationDetailsForLater(OrganisationDetailsViewModel model)
    {
        bool validationResult = await ValidateModel(model);

        if (!validationResult)
        {
            return View(nameof(ParticipatingOrganisationDetails), model);
        }

        var baseModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        var participatingOrganisationAnswers = model.ToAnswersDto();

        var saveAnswersResponse = await respondentService
            .SaveModificationParticipatingOrganisationAnswers(participatingOrganisationAnswers);

        if (!saveAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveAnswersResponse);
        }

        if (baseModel.Status is ModificationStatus.ReviseAndAuthorise)
        {
            return RedirectToRoute("sws:modifications", new { baseModel.SponsorOrganisationUserId, baseModel.RtsId });
        }

        return RedirectToRoute(PostApprovalRoute, new { baseModel.ProjectRecordId });
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpPost]
    public async Task<IActionResult> SaveOrganisationDetails(OrganisationDetailsViewModel model)
    {
        bool validationResult = await ValidateModel(model);

        if (!validationResult)
        {
            return View(nameof(ParticipatingOrganisationDetails), model);
        }

        var participatingOrganisationAnswers = model.ToAnswersDto();

        var saveAnswersResponse = await respondentService
            .SaveModificationParticipatingOrganisationAnswers(participatingOrganisationAnswers);

        if (!saveAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveAnswersResponse);
        }

        return RedirectToRoute(OrganisationsListRoute);
    }

    private async Task<bool> ValidateModel(OrganisationDetailsViewModel model)
    {
        // Fetch the CMS question set that defines what metadata must be collected for this document.
        var additionalQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(OrganisationDetailsSection);

        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        questionnaire.UpdateWithAnswers(model.Questions, questionnaire.Questions);

        return await this.ValidateQuestionnaire(validator, questionnaire);
    }
}