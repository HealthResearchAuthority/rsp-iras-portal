using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
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
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator)
{
    private readonly IRespondentService _respondentService = respondentService;

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

        var (result, modification) = await PrepareModificationAsync(projectModificationId, irasId, shortTitle, projectRecordId);
        if (result is not null)
        {
            return result;
        }

        // validate and update the status and answers for the change
        modification.ModificationChanges = await UpdateModificationChanges(projectRecordId, modification.ModificationChanges);

        // Set the 'ready for submission' flag if all changes are ready
        if (modification.ModificationChanges.All(c => c.ChangeStatus == ModificationStatus.ChangeReadyForSubmission))
        {
            modification.ChangesReadyForSubmission = true;
        }

        // Render the details view
        return View(modification);
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
            var respondentServiceResponse = await _respondentService.GetModificationChangeAnswers(modificationChange.ModificationChangeId, projectRecordId);

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
                modificationChange.ChangeStatus = ModificationStatus.ChangeReadyForSubmission;
            }

            // Apply respondent answers to the questionnaire
            questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

            // Validate the questionnaire (mandatory-only) using FluentValidation
            var result = await this.ValidateQuestionnaire(validator, questionnaire, true, false);

            modificationChange.ChangeStatus = result ?
                ModificationStatus.ChangeReadyForSubmission :
                ModificationStatus.Unfinished;

            // show surfacing questions
            ModificationHelpers.ShowSurfacingQuestion(questions, modificationChange, nameof(ModificationDetails));

            // remove all questions as they are not needed in the details view
            // they are only needed to calculate the ranking
            modificationChange.Questions.Clear();
        }

        return modificationChanges;
    }
}