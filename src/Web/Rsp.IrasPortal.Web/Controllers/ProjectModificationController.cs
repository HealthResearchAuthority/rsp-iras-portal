using System.Data;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsApplicant")]
public partial class ProjectModificationController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    IQuestionSetService questionSetService,
    IRtsService rtsService,
    IValidator<AreaOfChangeViewModel> areaofChangeValidator,
    IValidator<SearchOrganisationViewModel> searchOrganisationValidator,
    IValidator<DateViewModel> dateViewModelValidator,
    IValidator<PlannedEndDateOrganisationTypeViewModel> organisationTypeValidator,
    IValidator<AffectingOrganisationsViewModel> affectingOrgsValidator,
    IBlobStorageService blobStorageService
) : Controller

{
    private const string SelectAreaOfChange = "Select area of change";
    private const string SelectSpecificAreaOfChange = "Select specific change";

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
        var versionsResponse = await questionSetService.GetVersions();

        // a published version of question set must exist
        // so that it can be used when saving the modification/changes
        if (!versionsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(versionsResponse);
        }

        var versions = versionsResponse.Content!;

        var publishedVersion = versions.SingleOrDefault(version => version.IsPublished)?.VersionId;

        if (publishedVersion == null)
        {
            return this.ServiceError(versionsResponse);
        }

        TempData[TempDataKeys.QuestionSetPublishedVersionId] = publishedVersion;

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
                AreaOfChangeOptions =
                [
                    new() { Text = SelectAreaOfChange, Value = "" }
                ],
                SpecificChangeOptions =
                [
                    new() { Text = SelectSpecificAreaOfChange, Value = "" }
                ]
            });
        }

        // Store the list of area of changes in TempData (as serialized JSON string)
        TempData[TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(response.Content);

        var viewModel = new AreaOfChangeViewModel
        {
            PageTitle = SelectAreaOfChange,
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            AreaOfChangeOptions = response.Content
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                .Prepend(new SelectListItem { Value = "", Text = SelectAreaOfChange }),
            SpecificChangeOptions = [new SelectListItem { Value = "", Text = SelectSpecificAreaOfChange }]
        };

        // Populate the dropdown options based on any existing selections
        return View(viewModel);
    }

    /// <summary>
    /// Returns the specific changes related to a selected Area of Change.
    /// Pulls data from session cache for performance.
    /// </summary>
    [HttpGet]
    public IActionResult GetSpecificChangesByAreaId(int areaOfChangeId)
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
        var specificChanges = selectedArea?.ModificationSpecificAreaOfChanges?.ToList() ?? [];

        // Create a SelectListItem list for the specific changes
        var selectList = new List<SelectListItem> { new() { Value = "", Text = SelectSpecificAreaOfChange } };

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

            TempData[TempDataKeys.ProjectModification.AreaOfChangeId] = model.AreaOfChangeId;
            TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = model.SpecificChangeId;
        }

        // Retrieve the journey type from the selected specific change
        var journeyType = GetJourneyTypeFromSession(model);

        // If no journey type is specified, return the AreaOfChange view
        return RedirectBasedOnJourneyType(journeyType, model);
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
    private static void PopulateDropdownOptions(AreaOfChangeViewModel model, List<GetAreaOfChangesResponse> areaOfChanges)
    {
        model.AreaOfChangeOptions = areaOfChanges
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name,
                Selected = a.Id == model.AreaOfChangeId
            })
            .Prepend(new SelectListItem { Value = "", Text = SelectAreaOfChange });

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
                .Prepend(new SelectListItem { Value = "", Text = SelectSpecificAreaOfChange })
                ?? [new() { Value = "", Text = SelectSpecificAreaOfChange }];
        }
        else
        {
            model.SpecificChangeOptions = [new SelectListItem { Value = "", Text = SelectSpecificAreaOfChange }];
        }
    }

    [NonAction]
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

        return RedirectToRoute(routeName);
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
    /// Retrieves the JourneyType of the selected specific change from session.
    /// </summary>
    private string? GetJourneyTypeFromSession(AreaOfChangeViewModel model)
    {
        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;
        if (string.IsNullOrWhiteSpace(tempDataString))
        {
            return null;
        }

        // Deserialize the area of changes from TempData
        var areaOfChanges = JsonSerializer.Deserialize<List<GetAreaOfChangesResponse>>(tempDataString)!;

        // Find the change based on the selected area and specific change IDs
        var areaOfChange = areaOfChanges
            .FirstOrDefault(a => a.Id == model.AreaOfChangeId);

        // Find the specific change based on the selected area and specific change IDs
        var selectedChange = areaOfChanges
            .FirstOrDefault(a => a.Id == model.AreaOfChangeId)?
            .ModificationSpecificAreaOfChanges?
            .FirstOrDefault(sc => sc.Id == model.SpecificChangeId);

        // Store the name of the specific area of change in TempData for later use
        TempData[TempDataKeys.ProjectModification.AreaOfChangeText] = areaOfChange?.Name ?? string.Empty;
        TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = selectedChange?.Name ?? string.Empty;
        return selectedChange?.JourneyType;
    }

    /// <summary>
    /// Redirects user to the appropriate screen based on the journey type of their selection.
    /// </summary>
    private IActionResult RedirectBasedOnJourneyType(string? journeyType, AreaOfChangeViewModel model)
    {
        // If no journey type is specified, return the AreaOfChange view
        return journeyType switch
        {
            ModificationJourneyTypes.ParticipatingOrganisation => RedirectToAction(nameof(ParticipatingOrganisation)),
            ModificationJourneyTypes.PlannedEndDate => RedirectToAction(nameof(PlannedEndDate)),
            ModificationJourneyTypes.ProjectDocument => RedirectToAction(nameof(ProjectDocument)),
            _ => View(nameof(AreaOfChange), model)
        };
    }
}