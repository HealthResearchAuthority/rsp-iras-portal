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
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsUser")]
public class ProjectModificationController
(
    IProjectModificationsService projectModificationsService,
    IValidator<AreaOfChangeViewModel> areaofChangeValidator,
    IValidator<SearchOrganisationViewModel> searchOrganisationValidator
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

        // Check if required TempData values are present
        if (TempData.Peek(TempDataKeys.ProjectRecordId) is not string projectRecordId || IrasId == null)
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
        TempData[TempDataKeys.ProjectModificationId] = projectModification.Id;
        TempData[TempDataKeys.ProjectModificationIdentifier] = projectModification.ModificationIdentifier;
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
        var response = await projectModificationsService.GetAreaOfChanges();

        // If the service call failed, return a generic error page
        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        // Handle case when no area of change data is returned from service
        if (response.Content == null)
        {
            return View(new AreaOfChangeViewModel
            {
                AreaOfChangeOptions = new List<SelectListItem>
                {
                    new() { Text = "Select", Value = "" }
                },
                SpecificChangeOptions = new List<SelectListItem>
                {
                    new() { Text = "Select", Value = "" }
                }
            });
        }

        // Store the list of area of changes in TempData (as serialized JSON string)
        TempData[TempDataKeys.AreaOfChanges] = JsonSerializer.Serialize(response.Content);

        var viewModel = new AreaOfChangeViewModel
        {
            PageTitle = "Select area of change",
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            AreaOfChangeOptions = response.Content
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                .Prepend(new SelectListItem { Value = "", Text = "Select" }),
            SpecificChangeOptions = new List<SelectListItem> { new SelectListItem { Value = "", Text = "Select" } }
        };

        return View(viewModel);
    }

    /// <summary>
    /// Returns the specific changes related to a selected Area of Change.
    /// Pulls data from session cache for performance.
    /// </summary>
    [HttpGet]
    public IActionResult GetSpecificChangesByAreaId(int areaOfChangeId)
    {
        var tempDataString = TempData.Peek(TempDataKeys.AreaOfChanges) as string;

        if (string.IsNullOrWhiteSpace(tempDataString))
            return BadRequest("Area of changes not available.");

        var areaOfChanges = JsonSerializer.Deserialize<List<GetAreaOfChangesResponse>>(tempDataString)!;
        var selectedArea = areaOfChanges.FirstOrDefault(a => a.Id == areaOfChangeId);

        var specificChanges = selectedArea?.ModificationSpecificAreaOfChanges?.ToList() ?? new List<ModificationSpecificAreaOfChangeDto>();

        var selectList = new List<SelectListItem> { new SelectListItem { Value = "", Text = "Select" } };

        selectList.AddRange(specificChanges.Select(sc => new SelectListItem
        {
            Value = sc.Id.ToString(),
            Text = sc.Name
        }));

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

        var modificationId = TempData.Peek(TempDataKeys.ProjectModificationId);
        if (modificationId is Guid newGuid && newGuid != Guid.Empty)
        {
            await SaveModificationChange(model, newGuid);
        }

        var journeyType = GetJourneyTypeFromSession(model);
        return RedirectBasedOnJourneyType(journeyType, model);
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
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string ?? string.Empty
        };

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
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string ?? string.Empty
        };

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

        var tempDataString = TempData.Peek(TempDataKeys.AreaOfChanges) as string;
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
            .Prepend(new SelectListItem { Value = "", Text = "Select" });

        if (model.AreaOfChangeId.HasValue && model.AreaOfChangeId.Value != 0)
        {
            var selectedArea = areaOfChanges.FirstOrDefault(a => a.Id == model.AreaOfChangeId.Value);
            model.SpecificChangeOptions = selectedArea?.ModificationSpecificAreaOfChanges?
                .Select(sc => new SelectListItem
                {
                    Value = sc.Id.ToString(),
                    Text = sc.Name,
                    Selected = sc.Id == model.SpecificChangeId
                })
                .Prepend(new SelectListItem { Value = "", Text = "Select" })
                ?? [new() { Value = "", Text = "Select" }];
        }
        else
        {
            model.SpecificChangeOptions = [new SelectListItem { Value = "", Text = "Select" }];
        }
    }

    /// <summary>
    /// Saves a new ProjectModificationChange record to the backend based on user selection.
    /// </summary>
    private async Task SaveModificationChange(AreaOfChangeViewModel model, Guid modificationId)
    {
        var respondent = this.GetRespondentFromContext();
        var name = $"{respondent.GivenName} {respondent.FamilyName}";

        var modificationChangeResponse = await projectModificationsService.CreateModificationChange(new ProjectModificationChangeRequest
        {
            AreaOfChange = model.AreaOfChangeId.ToString(),
            SpecificAreaOfChange = model.SpecificChangeId.ToString(),
            ProjectModificationId = modificationId,
            Status = "OPEN",
            CreatedBy = name,
            UpdatedBy = name
        });

        if (modificationChangeResponse.IsSuccessStatusCode)
        {
            var modificationChange = modificationChangeResponse.Content!;
            TempData[TempDataKeys.ProjectModificationChangeId] = modificationChange.Id;
        }
    }

    /// <summary>
    /// Retrieves the JourneyType of the selected specific change from session.
    /// </summary>
    private string? GetJourneyTypeFromSession(AreaOfChangeViewModel model)
    {
        var tempDataString = TempData.Peek(TempDataKeys.AreaOfChanges) as string;
        if (string.IsNullOrWhiteSpace(tempDataString))
        {
            return null;
        }

        var areaOfChanges = JsonSerializer.Deserialize<List<GetAreaOfChangesResponse>>(tempDataString)!;
        var selectedChange = areaOfChanges
            .FirstOrDefault(a => a.Id == model.AreaOfChangeId)?
            .ModificationSpecificAreaOfChanges?
            .FirstOrDefault(sc => sc.Id == model.SpecificChangeId);

        TempData[TempDataKeys.SpecificAreaOfChangeText] = selectedChange?.Name ?? string.Empty;
        return selectedChange?.JourneyType;
    }

    /// <summary>
    /// Redirects user to the appropriate screen based on the journey type of their selection.
    /// </summary>
    private IActionResult RedirectBasedOnJourneyType(string? journeyType, AreaOfChangeViewModel model)
    {
        return journeyType switch
        {
            ModificationJourneyTypes.ParticipatingOrganisation => RedirectToAction(nameof(ParticipatingOrganisation)),
            ModificationJourneyTypes.PlannedEndDate => RedirectToAction(nameof(PlannedEndDate)),
            ModificationJourneyTypes.ProjectDocument => RedirectToAction("ProjectDocument", "ProjectModification"),
            _ => View(nameof(AreaOfChange), model)
        };
    }
}