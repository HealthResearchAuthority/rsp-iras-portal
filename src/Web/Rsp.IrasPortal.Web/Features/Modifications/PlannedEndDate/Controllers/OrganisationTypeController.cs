using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.PlannedEndDate.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("modifications/plannedenddate/[action]", Name = "pmc:[action]")]
public class OrganisationTypeController
(
    IRespondentService respondentService,
    IValidator<PlannedEndDateOrganisationTypeViewModel> organisationTypeValidator
) : Controller
{
    /// <summary>
    /// Returns the view to select the organisation types for the planned end date change of the project.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult PlannedEndDateOrganisationType()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new PlannedEndDateOrganisationTypeViewModel());

        var selectedOrgTypes = TempData.Peek(TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType) as string;

        if (!string.IsNullOrEmpty(selectedOrgTypes))
        {
            viewModel.SelectedOrganisationTypes = JsonSerializer.Deserialize<List<string>>(selectedOrgTypes)!;
        }

        // Redirect to the PlannedEndDateOrganisationType view with the populated view model
        return View(viewModel);
    }

    /// <summary>
    /// Handles the POST request for submitting selected organisation types for the planned end date change.
    /// Validates the input model, processes the selected organisation types, and saves the respondent's answers.
    /// </summary>
    /// <param name="model">The view model containing the selected organisation types.</param>
    /// <returns>
    /// If validation fails, returns the same view with validation errors.
    /// If successful, saves the answers and redirects to the next step.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> SubmitOrganisationTypes(PlannedEndDateOrganisationTypeViewModel model, bool saveForLater = false)
    {
        // Validate the incoming model using the injected validator
        var validationResult = await organisationTypeValidator.ValidateAsync(new ValidationContext<PlannedEndDateOrganisationTypeViewModel>(model));

        if (!validationResult.IsValid)
        {
            // Add validation errors to the ModelState to display them in the view
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            // Return the view with the model and validation errors
            return View(nameof(PlannedEndDateOrganisationType), model);
        }

        // Map the selected organisation type display values back to their unique keys
        var selectedOrgTypes = model.SelectedOrganisationTypes.ConvertAll
        (
            org => model
                            .OrganisationTypes
                            .Single(kv => kv.Value == org).Key
        );

        // Retrieve the published version ID from TempData
        var publishedVersion = (TempData.Peek(TempDataKeys.QuestionSetPublishedVersionId) as string)!;

        if (selectedOrgTypes.Count > 0)
        {
            TempData[TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType] = JsonSerializer.Serialize(model.SelectedOrganisationTypes);
        }

        var routeName = saveForLater ? "pov:postapproval" : "pmc:affectingorganisations";

        // Save the respondent's answers and redirect to the next step
        return await SaveModificationAnswers([new RespondentAnswerDto
        {
            QuestionId = QuestionIds.AffectingOrganisationsType,
            VersionId = publishedVersion,
            OptionType = selectedOrgTypes.Count switch
            {
                1 => "Single",
                > 1 => "Multiple",
                _ => null
            },
            SelectedOption = selectedOrgTypes switch
            {
                { Count: > 0 } => string.Join(",", selectedOrgTypes),
                _ => null
            },
            CategoryId = QuestionCategories.PlannedEndDate,
            SectionId = QuestionSections.AffectingOrganisationsType
        }], routeName);
    }

    public async Task<IActionResult> SaveModificationAnswers(List<RespondentAnswerDto> respondentAnswers, string routeName)
    {
        // Get required modification data for saving the planned end date
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Build the request to save modification answers
        var request = new ProjectModificationAnswersRequest
        {
            ProjectModificationChangeId = projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
            ProjectRecordId = projectRecordId,
            ProjectPersonnelId = respondentId
        };

        // Add the new planned end date answer to the request
        request.ModificationAnswers.AddRange(respondentAnswers);

        // Save the modification answers using the respondent service
        var saveModificationAnswersResponse = await respondentService.SaveModificationAnswers(request);

        if (!saveModificationAnswersResponse.IsSuccessStatusCode)
        {
            // Return a service error view if saving fails
            return this.ServiceError(saveModificationAnswersResponse);
        }

        if (routeName == "pov:postapproval")
        {
            return RedirectToRoute(routeName, new { projectRecordId });
        }

        return RedirectToRoute(routeName);
    }
}