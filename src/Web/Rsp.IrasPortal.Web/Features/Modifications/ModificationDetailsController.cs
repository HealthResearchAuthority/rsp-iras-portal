using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications;

[Route("/modifications/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ModificationDetailsController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<QuestionnaireViewModel> validator
) : Controller
{
    /// <summary>
    /// Displays the modification details page for a given project modification.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record that owns the modification.</param>
    /// <param name="irasId">The IRAS identifier for display in the page header.</param>
    /// <param name="shortTitle">The project's short title for display in the page header.</param>
    /// <param name="projectModificationId">The unique identifier of the project modification to display.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> that renders the details view when successful; otherwise,
    /// an error result produced by <c>this.ServiceError(...)</c> when a service call fails.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> ModificationDetails(string projectRecordId, string irasId, string shortTitle, Guid projectModificationId)
    {
        // all changes are being reviewing here so remove the change specific keys from tempdata
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChanges);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeText);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeText);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeMarker);

        TempData[TempDataKeys.IrasId] = irasId;

        // Fetch the modification by its identifier
        var modificationResponse = await projectModificationsService.GetModificationsByIds([projectModificationId.ToString()]);

        // Short-circuit with a service error if the call failed
        if (!modificationResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(modificationResponse);
        }

        if (modificationResponse.Content?.Modifications.Any() is false)
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = $"Error retrieving the modification for project record: {projectRecordId} modificationId: {projectModificationId.ToString()}",
            });
        }

        // Select the first (and only) modification result
        var modification = modificationResponse.Content!.Modifications.First();

        // Build the base view model with project metadata
        var viewModel = new ModificationDetailsViewModel
        {
            ModificationId = modification.Id,
            IrasId = irasId,
            ShortTitle = shortTitle,
            ModificationIdentifier = modification.ModificationId,
            Status = modification.Status,
            ProjectRecordId = projectRecordId
        };

        // Persist the modification identifier in TempData for subsequent requests/pages
        TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modification.ModificationId;
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = modification.Id;

        // Retrieve all changes related to this modification
        var modificationsResponse = await projectModificationsService.GetModificationChanges(Guid.Parse(viewModel.ModificationId));

        if (!modificationsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(modificationsResponse);
        }

        // These fields are currently set to fixed values (could be driven by data in future)
        viewModel.ModificationType = "Minor modification";
        viewModel.Category = "{A > B/C > B > C > New site > N/A}";
        viewModel.ReviewType = "No review required";

        // Load initial questions to resolve display names for areas of change
        var initialQuestionsResponse = await cmsQuestionsetService.GetInitialModificationQuestions();

        if (!initialQuestionsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(initialQuestionsResponse);
        }

        var initialQuestions = initialQuestionsResponse.Content!;

        // modification changes returned from the service
        var modificationChanges = modificationsResponse.Content!;

        // Map raw changes to view models, resolving area names using the initial questions
        viewModel.ModificationChanges =
            (from change in modificationChanges
             let areaOfChange = initialQuestions.AreasOfChange.Find(area => area.AutoGeneratedId == change.AreaOfChange)
             let specificAreaOfChange = areaOfChange?.SpecificAreasOfChange.Find(area => area.AutoGeneratedId == change.SpecificAreaOfChange)
             // TODO: Include the project documents once the decision is made on how to include these
             // exclude project documents for now as it can't be displayed in the same format
             where areaOfChange.OptionName?.Equals("project documents", StringComparison.OrdinalIgnoreCase) is false
             select new ModificationChangeModel
             {
                 ModificationChangeId = change.Id,
                 ModificationType = "Minor Modification",
                 Category = "A > B/C",
                 ReviewType = "No review required",
                 AreaOfChangeName = areaOfChange?.OptionName ?? string.Empty,
                 SpecificChangeName = specificAreaOfChange?.OptionName ?? string.Empty,
                 SpecificAreaOfChangeId = specificAreaOfChange?.AutoGeneratedId ?? string.Empty,
                 ChangeStatus = change.Status
             }).ToList();

        // validate and update the status and answers for the change
        viewModel.ModificationChanges = await UpdateModificationChanges(projectRecordId, viewModel.ModificationChanges.ToList());

        // Set the 'ready for submission' flag if all changes are ready
        if (viewModel.ModificationChanges.All(c => c.ChangeStatus == "Change ready for submission"))
        {
            viewModel.ChangesReadyForSubmission = true;
        }

        // Render the details view
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult UnfinishedChanges()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsViewModel());

        return View("UnfinishedChanges", viewModel);
    }

    [HttpGet]
    public IActionResult ConfirmRemoveChange(string modificationChangeId, string modificationChangeName)
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsViewModel());

        viewModel.ModificationChangeId = modificationChangeId;
        viewModel.SpecificAreaOfChange = modificationChangeName;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveChange(Guid modificationChangeId, string modificationChangeName)
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsViewModel());

        var removeChangeResponse = await projectModificationsService.RemoveModificationChange(modificationChangeId);

        if (!removeChangeResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(removeChangeResponse);
        }

        TempData[TempDataKeys.ProjectModificationChange.ChangeRemoved] = true;
        TempData[TempDataKeys.ProjectModificationChange.ChangeName] = modificationChangeName;

        return RedirectToAction(nameof(ModificationDetails), new
        {
            projectRecordId = viewModel.ProjectRecordId,
            irasId = viewModel.IrasId,
            shortTitle = viewModel.ShortTitle,
            projectModificationId = viewModel.ModificationId
        });
    }

    private async Task<List<ModificationChangeModel>> UpdateModificationChanges(string projectRecordId, List<ModificationChangeModel> modificationChanges)
    {
        foreach (var modificationChange in modificationChanges)
        {
            // get the responent answers for the category
            var respondentServiceResponse = await respondentService.GetModificationChangeAnswers(modificationChange.ModificationChangeId, projectRecordId);

            // get the questions for the modification journey
            var questionSetServiceResponse = await cmsQuestionsetService.GetModificationsJourney(modificationChange.SpecificAreaOfChangeId);

            // return the error view if unsuccessfull
            if (!respondentServiceResponse.IsSuccessStatusCode || !questionSetServiceResponse.IsSuccessStatusCode)
            {
                // return the modificationChanges unchanged in case of error
                return modificationChanges;
            }

            var respondentAnswers = respondentServiceResponse.Content!;

            // convert the questions response to QuestionnaireViewModel
            var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content!);

            var questions = questionnaire.Questions;

            if (questions.Count == 0)
            {
                modificationChange.ChangeStatus = "Change ready for submission";
            }

            // Apply respondent answers to the questionnaire and trim conditional/unanswered questions
            var (surfacingQuestion, showSurfacingQuestion) = ModificationHelpers.ApplyRespondentAnswersAndTrim(questionnaire, respondentAnswers, nameof(ModificationDetails));

            // Validate the questionnaire (mandatory-only) using FluentValidation
            var context = new ValidationContext<QuestionnaireViewModel>(questionnaire);
            context.RootContextData["questions"] = questionnaire.Questions;
            context.RootContextData["ValidateMandatoryOnly"] = true;

            var result = await validator.ValidateAsync(context);

            modificationChange.ChangeStatus = result.IsValid ?
                "Change ready for submission" :
                "Unfinished";

            // If the surfacing question should be shown for ModificationDetails, capture its display text
            if (showSurfacingQuestion && surfacingQuestion != null)
            {
                modificationChange.SpecificChangeAnswer = surfacingQuestion.GetDisplayText(false);
            }
        }

        return modificationChanges;
    }
}