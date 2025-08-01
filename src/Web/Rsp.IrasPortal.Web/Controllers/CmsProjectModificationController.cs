using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("[controller]/[action]", Name = "cmspmc:[action]")]
[Authorize(Policy = "IsUser")]
public class CmsProjectModificationController
(
    IProjectModificationsService projectModificationsService,
    IValidator<AreaOfChangeViewModel> areaofChangeValidator,
    IValidator<SearchOrganisationViewModel> searchOrganisationValidator,
    ICmsQuestionSetServiceClient cmsQuestionsetClient
) : Controller
{
    /// <summary>
    /// Initiates the creation of a new project modification.
    /// </summary>
    /// <param name="separator">Separator to use in the modification identifier. Default is "/".</param>
    /// <returns>Redirects to the resume route if successful, otherwise returns an error page.</returns>
    [HttpGet]
    public async Task<IActionResult> CreateModification(string separator = "/")
    {
        // Retrieve IRAS ID from TempData
        var IrasId = TempData.Peek(TempDataKeys.IrasId) as int?;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId).ToString();

        // Check if required TempData values are present
        if (string.IsNullOrEmpty(projectRecordId) || IrasId == null)
        {
            // Return a problem response if data is missing
            var problemDetails = this.ProblemResult(new ServiceResponse
            {
                Error = "ProjectRecordId and/or IrasId missing",
                StatusCode = HttpStatusCode.BadRequest,
                ReasonPhrase = "Bad Request"
            });

            return Problem(problemDetails.Detail, problemDetails.Instance, problemDetails.Status, problemDetails.Title, problemDetails.Type);
        }

        // Get respondent information from the current context
        var respondent = this.GetRespondentFromContext();

        // Compose the full name of the respondent
        var name = $"{respondent.GivenName} {respondent.FamilyName}";

        // Create a new project modification request
        var modificationRequest = new ProjectModificationRequest
        {
            ProjectRecordId = projectRecordId,
            ModificationIdentifier = IrasId + separator,
            Status = "OPEN",
            CreatedBy = name,
            UpdatedBy = name
        };

        // Call the service to create the modification
        var projectModificationServiceResponse = await projectModificationsService.CreateModification(modificationRequest);

        // If the service call failed, return a generic error page
        if (!projectModificationServiceResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(projectModificationServiceResponse);
        }

        // Retrieve the created project modification from the response
        var projectModification = projectModificationServiceResponse.Content!;

        // Store relevant IDs in TempData for later use
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectModification.Id;
        TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = projectModification.ModificationIdentifier;
        TempData[TempDataKeys.CategoryId] = QuestionCategories.ProjectModification;

        return RedirectToAction(nameof(AreaOfChange));
    }

    /// <summary>
    /// Displays the Area of Change selection screen.
    /// Retrieves area of change data and stores it in session for later reuse.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AreaOfChange()
    {
        var response = await projectModificationsService.GetInitialQuestions();

        // If the service call failed, return a generic error page
        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        // Store the list of area of changes in TempData (as serialized JSON string)
        //TempData[TempDataKeys.AreaOfChanges] = JsonSerializer.Serialize(response.Content);

        var viewModel = new AreaOfChangeViewModel
        {
            PageTitle = "Select area of change",
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            AreaOfChangeOptions = response.Content.AreaOfChange.Answers
                .Select(a => new SelectListItem { Value = a.Key.ToString(), Text = a.OptionName })
                .Prepend(new SelectListItem { Value = "", Text = "Select area of change" }),
            SpecificChangeOptions = response.Content.SpecificChange.Answers
                .Select(a => new SelectListItem { Value = a.Key.ToString(), Text = a.OptionName })
                .Prepend(new SelectListItem { Value = "", Text = "Select specific change" })
        };

        // Populate the dropdown options based on any existing selections
        return View(viewModel);
    }

    /// <summary>
    /// Returns the specific changes related to a selected Area of Change.
    /// Pulls data from session cache for performance.
    /// </summary>
    [HttpGet]
    public IActionResult GetSpecificChangesByAreaId(string areaOfChangeId)
    {
        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;

        if (string.IsNullOrWhiteSpace(tempDataString))
        {
            return BadRequest("Area of changes not available.");
        }

        // Deserialize the area of changes from TempData
        var areaOfChanges = JsonSerializer.Deserialize<List<GetAreaOfChangesResponse>>(tempDataString)!;

        // Find the specific area of change based on the provided ID
        var selectedArea = areaOfChanges.FirstOrDefault(a => a.Id == areaOfChangeId);

        // If no area is found, return an empty list
        var specificChanges = selectedArea?.ModificationSpecificAreaOfChanges?.ToList() ?? new List<ModificationSpecificAreaOfChangeDto>();

        // Create a SelectListItem list for the specific changes
        var selectList = new List<SelectListItem> { new SelectListItem { Value = "", Text = "Select specific change" } };

        // Add each specific change to the SelectListItem list
        selectList.AddRange(specificChanges.Select(sc => new SelectListItem
        {
            Value = sc.Id.ToString(),
            Text = sc.Name
        }));

        // Return the list as a JSON result
        return Json(selectList);
    }

    /// <summary>
    /// Processes user’s selection of Area and Specific Change and redirects based on journey type.
    /// Saves the modification change to backend and handles validation.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ConfirmModificationJourney(AreaOfChangeViewModel model)
    {
        var validationResult = await areaofChangeValidator.ValidateAsync(new ValidationContext<AreaOfChangeViewModel>(model));

        if (!validationResult.IsValid)
        {
            HandleValidationErrors(validationResult, model);
            return View(nameof(AreaOfChange), model);
        }

        var modificationId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId);
        if (modificationId is Guid newGuid && newGuid != Guid.Empty)
        {
            await SaveModificationChange(model, newGuid);
        }

        // Retrieve the journey type from the selected specific change
        var journeyQuestionset = await cmsQuestionsetClient.GetModificationsJourney(model.SpecificChangeId);
        var questionsetObject = QuestionsetHelpers.BuildQuestionnaireViewModel(journeyQuestionset.Content);

        // If no journey type is specified, return the AreaOfChange view

        // TODO handle questionset
        return View("RenderQuestions", questionsetObject);
    }

    /// <summary>
    /// Returns the view for selecting a participating organisation.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult ParticipatingOrganisation()
    {
        var viewModel = new SearchOrganisationViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeText) as string ?? string.Empty
        };

        // Retrieve the current organisation search term from TempData
        return View(nameof(SearchOrganisation), viewModel);
    }

    /// <summary>
    /// Returns the view to update the planned end date of the project.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult PlannedEndDate()
    {
        var viewModel = new PlannedEndDateViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeText) as string ?? string.Empty
        };

        // Retrieve the current planned end date from TempData
        return View(nameof(PlannedEndDate), viewModel);
    }

    /// <summary>
    /// Handles search form submission for participant organisation lookup.
    /// Validates the search model and returns the view with errors if needed.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SearchOrganisation(SearchOrganisationViewModel model)
    {
        var validationResult = await searchOrganisationValidator.ValidateAsync(new ValidationContext<SearchOrganisationViewModel>(model));
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        // If there are validation errors, repopulate dropdowns from session
        return View(nameof(SearchOrganisation), model);
    }

    /// <summary>
    /// Adds validation errors to ModelState and rebuilds dropdowns from session.
    /// </summary>
    private void HandleValidationErrors(ValidationResult validationResult, AreaOfChangeViewModel model)
    {
        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;
        if (!string.IsNullOrWhiteSpace(tempDataString))
        {
            var areaOfChanges = JsonSerializer.Deserialize<List<GetAreaOfChangesResponse>>(tempDataString)!;
            PopulateDropdownOptions(model, areaOfChanges);
        }
    }

    /// <summary>
    /// Populates AreaOfChange and SpecificChange dropdowns based on current selection.
    /// </summary>
    private void PopulateDropdownOptions(AreaOfChangeViewModel model, List<GetAreaOfChangesResponse> areaOfChanges)
    {
        model.AreaOfChangeOptions = areaOfChanges
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name,
                Selected = a.Id == model.AreaOfChangeId
            })
            .Prepend(new SelectListItem { Value = "", Text = "Select area of change" });

        if (model.AreaOfChangeId != null)
        {
            var selectedArea = areaOfChanges.FirstOrDefault(a => a.Id == model.AreaOfChangeId);
            model.SpecificChangeOptions = selectedArea?.ModificationSpecificAreaOfChanges?
                .Select(sc => new SelectListItem
                {
                    Value = sc.Id.ToString(),
                    Text = sc.Name,
                    Selected = sc.Id.ToString() == model.SpecificChangeId
                })
                .Prepend(new SelectListItem { Value = "", Text = "Select specific change" })
                ?? [new() { Value = "", Text = "Select specific change" }];
        }
        else
        {
            model.SpecificChangeOptions = [new SelectListItem { Value = "", Text = "Select specific change" }];
        }
    }

    /// <summary>
    /// Saves a new ProjectModificationChange record to the backend based on user selection.
    /// </summary>
    private async Task SaveModificationChange(AreaOfChangeViewModel model, Guid modificationId)
    {
        var respondent = this.GetRespondentFromContext();
        var name = $"{respondent.GivenName} {respondent.FamilyName}";

        // Create a new ProjectModificationChangeRequest
        var modificationChangeResponse = await projectModificationsService.CreateModificationChange(new ProjectModificationChangeRequest
        {
            AreaOfChange = model.AreaOfChangeId.ToString(),
            SpecificAreaOfChange = model.SpecificChangeId.ToString(),
            ProjectModificationId = modificationId,
            Status = "OPEN",
            CreatedBy = name,
            UpdatedBy = name
        });

        // Store the modification change ID in TempData for later use
        if (modificationChangeResponse.IsSuccessStatusCode)
        {
            var modificationChange = modificationChangeResponse.Content!;
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChange.Id;
        }
    }

    /// <summary>
    /// Redirects user to the appropriate screen based on the journey type of their selection.
    /// </summary>
    private IActionResult RedirectBasedOnJourneyType(string? journeyType, AreaOfChangeViewModel model)
    {
        // Handle questionset based on section
        return journeyType switch
        {
            ModificationJourneyTypes.ParticipatingOrganisation => RedirectToAction(nameof(ParticipatingOrganisation)),
            ModificationJourneyTypes.PlannedEndDate => RedirectToAction(nameof(PlannedEndDate)),
            ModificationJourneyTypes.ProjectDocument => RedirectToAction("ProjectDocument", "ProjectModification"),
            _ => View(nameof(AreaOfChange), model)
        };
    }
}