using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
public partial class ProjectModificationController
{
    /// <summary>
    /// Returns the view to select the organisation types for the planned end date change of the project.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult AffectingOrganisations()
    {
        // Create a new view model and populate its properties from TempData for continuity across requests
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new AffectingOrganisationsViewModel());

        // Retrieve previously selected organisation locations from TempData, if any
        var selectedLocations = TempData.Peek(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsLocations) as string;

        // If locations exist, deserialize and assign to the view model
        if (!string.IsNullOrEmpty(selectedLocations))
        {
            viewModel.SelectedLocations = JsonSerializer.Deserialize<List<string>>(selectedLocations)!;
        }

        // Retrieve the user's previous selection for "all or some organisations" from TempData
        var affectedAllOrSomeOrgs = TempData.Peek(TempDataKeys.ProjectModificationPlannedEndDate.AffectedAllOrSomeOrganisations) as string;

        // If a selection exists, assign it to the view model
        if (!string.IsNullOrEmpty(affectedAllOrSomeOrgs))
        {
            viewModel.SelectedAffectedOrganisations = affectedAllOrSomeOrgs;
        }

        // Retrieve the user's previous selection for "require additional resources" from TempData
        var affectedOrgsRequireAdditionalResources = TempData.Peek(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsRequireAdditionalResources) as string;

        // If a selection exists, assign it to the view model
        if (!string.IsNullOrEmpty(affectedOrgsRequireAdditionalResources))
        {
            viewModel.SelectedAdditionalResources = affectedOrgsRequireAdditionalResources;
        }

        // Return the view with the populated view model for user interaction
        return View(viewModel);
    }

    /// <summary>
    /// Handles the POST request to save the user's responses for affected organisations.
    /// Performs validation (unless saving for later), persists answers to TempData and backend, and redirects accordingly.
    /// </summary>
    /// <param name="model">The view model containing the user's selections for affected organisations.</param>
    /// <param name="saveForLater">True if the user chose to save progress for later; otherwise, false.</param>
    /// <returns>Redirects to the next step in the workflow or returns the view with validation errors.</returns>
    [HttpPost]
    public async Task<IActionResult> SaveAffectedOrganisationsResponses(AffectingOrganisationsViewModel model, bool saveForLater)
    {
        // Prepare validation context for the submitted model
        var validationContext = new ValidationContext<AffectingOrganisationsViewModel>(model);

        // Retrieve the selected organisation types from TempData for context-sensitive validation
        var selectedOrgTypes = TempData.Peek(TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType) as string;

        var selectedOrgs = new List<string>();

        // If organisation types are present, deserialize them for use in validation
        if (!string.IsNullOrEmpty(selectedOrgTypes))
        {
            selectedOrgs = JsonSerializer.Deserialize<List<string>>(selectedOrgTypes)!;
        }

        // Only perform validation if the user is not saving for later
        if (!saveForLater)
        {
            // If "NHS/HSC" is among the selected organisations, set a flag in the validation context
            if (!string.IsNullOrWhiteSpace(selectedOrgs.FirstOrDefault(org => org == "NHS/HSC")))
            {
                validationContext.RootContextData[ValidationKeys.ProjectModificationPlannedEndDate.AffectedOrganisations] = true;
            }

            // Validate the model using the affectingOrgsValidator
            var validationResult = affectingOrgsValidator.Validate(validationContext);

            // If validation fails, add errors to ModelState and return the view with errors
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                return View(nameof(AffectingOrganisations), model);
            }
        }

        // Build the list of answers from the model for persistence
        var answers = BuildAnswers(model);

        // Choose the next route based on whether the user is saving for later or continuing
        var routeName = saveForLater ? "pov:projectdetails" : "pmc:modificationchangesreview";

        // Save the answers and redirect to the appropriate route
        return await SaveModificationAnswers(answers, routeName);
    }

    /// <summary>
    /// Builds a list of RespondentAnswerDto objects from the AffectingOrganisationsViewModel.
    /// Persists relevant selections to TempData for continuity across requests.
    /// </summary>
    /// <param name="model">The view model containing user selections for affected organisations.</param>
    /// <returns>A list of RespondentAnswerDto representing the user's answers.</returns>
    private List<RespondentAnswerDto> BuildAnswers(AffectingOrganisationsViewModel model)
    {
        var respondentAnswers = new List<RespondentAnswerDto>();

        // Retrieve the published version ID from TempData for answer versioning
        var publishedVersion = (TempData.Peek(TempDataKeys.QuestionSetPublishedVersionId) as string)!;

        // Remove any previous locations from TempData to avoid stale data
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsLocations);

        // If locations are selected, serialize and store them in TempData
        if (model.SelectedLocations.Count > 0)
        {
            TempData[TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsLocations] = JsonSerializer.Serialize(model.SelectedLocations);
        }

        // Convert selected location display values to their corresponding keys (organisation codes)
        var selectedLocations = model.SelectedLocations.ConvertAll
        (
            org => model
                            .ParticipatingOrganisationsLocations
                            .Single(kv => kv.Value == org).Key
        );

        // Add answer for participating organisations' locations
        respondentAnswers.Add(new RespondentAnswerDto
        {
            QuestionId = QuestionIds.ParticipatingOrganisationsLocation,
            VersionId = publishedVersion,
            OptionType = selectedLocations switch
            {
                { Count: 1 } => "Single",
                { Count: > 1 } => "Multiple",
                _ => null
            },
            SelectedOption = selectedLocations switch
            {
                { Count: > 0 } => string.Join(",", selectedLocations),
                _ => null
            },
            CategoryId = QuestionCategories.PlannedEndDate,
            SectionId = QuestionSections.AffectingOrganisationsMetaData
        });

        // Remove any previous "all or some organisations" selection from TempData
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedAllOrSomeOrganisations);

        // If a selection is made, store it in TempData
        if (!string.IsNullOrWhiteSpace(model.SelectedAffectedOrganisations))
        {
            TempData[TempDataKeys.ProjectModificationPlannedEndDate.AffectedAllOrSomeOrganisations] = model.SelectedAffectedOrganisations;
        }

        // Add answer for whether all or some organisations are affected
        respondentAnswers.Add(new RespondentAnswerDto
        {
            QuestionId = QuestionIds.ParticipatingOrganisationsAllOrSome,
            VersionId = publishedVersion,
            OptionType = "Single",
            SelectedOption = !string.IsNullOrWhiteSpace(model.SelectedAffectedOrganisations) ? model.SelectedAffectedOrganisations : null,
            CategoryId = QuestionCategories.PlannedEndDate,
            SectionId = QuestionSections.AffectingOrganisationsMetaData
        });

        // Remove any previous "require additional resources" selection from TempData
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsRequireAdditionalResources);

        // If a selection is made, store it in TempData
        if (!string.IsNullOrWhiteSpace(model.SelectedAdditionalResources))
        {
            TempData[TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsRequireAdditionalResources] = model.SelectedAdditionalResources;
        }

        // Add answer for whether affected organisations require additional resources
        respondentAnswers.Add(new RespondentAnswerDto
        {
            QuestionId = QuestionIds.ParticipatingOrganisationsAdditionalResources,
            VersionId = publishedVersion,
            OptionType = "Single",
            SelectedOption = !string.IsNullOrWhiteSpace(model.SelectedAdditionalResources) ? model.SelectedAdditionalResources : null,
            CategoryId = QuestionCategories.PlannedEndDate,
            SectionId = QuestionSections.AffectingOrganisationsMetaData
        });

        return respondentAnswers;
    }
}