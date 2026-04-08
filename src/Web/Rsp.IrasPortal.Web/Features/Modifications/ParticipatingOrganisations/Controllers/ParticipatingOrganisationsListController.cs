using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;

/// <summary>
/// Controller responsible for handling participating organisations list
/// within project modifications.
/// </summary>
[Authorize(Policy = Workspaces.MyResearch)]
[Route("modifications/participatingorganisations/[action]", Name = "pol:[action]")]
public class ParticipatingOrganisationsListController
(
    ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService,
    IValidator<QuestionnaireViewModel> validator
) : Controller
{
    /// <summary>
    /// The CMS section identifier used to retrieve the participating organisation details question set.
    /// </summary>
    private const string OrganisationDetailsSection = "pom-participating-organisation-details";

    /// <summary>
    /// Returns the view for listing participating organisations.
    /// Restores previously selected organisations from TempData and
    /// evaluates the detail completion status for each organisation.
    /// </summary>
    /// <returns>The participating organisations list view populated with organisation data and completion statuses.</returns>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    public async Task<IActionResult> ParticipatingOrganisationsList()
    {
        // Populate base modification properties (e.g. project id, modification id) from TempData
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new OrganisationsListViewModel());

        // Retrieve the previously selected organisations stored as serialised JSON in TempData
        var selectedOrganistionsJson = TempData.Peek(TempDataKeys.ProjectModification.SelectedParticipatingOrganisations) as string;

        var selectedOrganisations = !string.IsNullOrEmpty(selectedOrganistionsJson) ?
            JsonSerializer.Deserialize<List<ParticipatingOrganisationModel>>(selectedOrganistionsJson) :
            [];

        // Determine the details completion status for each selected organisation
        if (selectedOrganisations?.Count > 0)
        {
            foreach (var org in selectedOrganisations)
            {
                org.DetailsStatus = await GetOrganisationDetailsCompletionStatus(org.OrganisationId)
                    ? nameof(OrganisationDetailsStatus.Complete)
                    : nameof(OrganisationDetailsStatus.Incomplete);
            }

            viewModel.Organisations = selectedOrganisations;
        }

        return View(viewModel);
    }

    /// <summary>
    /// Determines whether the organisation details for a given organisation are complete
    /// by fetching the CMS question set and evaluating the respondent's answers against it.
    /// </summary>
    /// <param name="organisationId">The unique identifier of the organisation to check.</param>
    /// <returns><see langword="true"/> if the organisation details are complete; otherwise, <see langword="false"/>.</returns>
    private async Task<bool> GetOrganisationDetailsCompletionStatus(Guid organisationId)
    {
        // Fetch the CMS question set that defines the required organisation detail fields
        var additionalQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(OrganisationDetailsSection);

        // Build a questionnaire view model from the CMS response to use for validation
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        return await EvaluateOrganisationDetailsCompletion(organisationId, questionnaire);
    }

    /// <summary>
    /// Evaluates whether a single organisation's details are complete by fetching saved answers
    /// and validating them against the provided questionnaire.
    /// </summary>
    /// <param name="organisationId">The unique identifier of the organisation whose answers are being evaluated.</param>
    /// <param name="questionnaire">The questionnaire view model containing the expected questions.</param>
    /// <param name="addModelErrors">
    /// When <see langword="true"/>, validation errors are added to <see cref="Controller.ModelState"/>.
    /// Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if no answers have been provided yet or if validation fails
    /// (indicating the details are incomplete); otherwise, <see langword="false"/>.
    /// </returns>
    private async Task<bool> EvaluateOrganisationDetailsCompletion(Guid organisationId, QuestionnaireViewModel questionnaire, bool addModelErrors = false)
    {
        // Fetch the respondent's saved answers for this organisation; default to empty if not found
        var answersResponse = await respondentService.GetModificationParticipatingOrganisationAnswers(organisationId);
        var answers = answersResponse?.StatusCode == HttpStatusCode.OK
            ? answersResponse.Content ?? []
            : [];

        // Merge saved answers into the questionnaire so validation runs against the current state
        questionnaire.UpdateWithRespondentAnswers(answers);

        // Validate the questionnaire; details are incomplete if there are no answers or validation fails
        var isValid = await this.ValidateQuestionnaire(validator, questionnaire, true, false);

        return isValid;
    }
}