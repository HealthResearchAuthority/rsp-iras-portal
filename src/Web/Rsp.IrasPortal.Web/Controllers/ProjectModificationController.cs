using System.Data;
using System.Net;
using System.Text.Json;
using Azure.Core;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsUser")]
public partial class ProjectModificationController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    IRtsService rtsService,
    IValidator<AreaOfChangeViewModel> areaofChangeValidator,
    IValidator<SearchOrganisationViewModel> searchOrganisationValidator,
    IValidator<DateViewModel> dateViewModelValidator,
    IValidator<PlannedEndDateOrganisationTypeViewModel> organisationTypeValidator,
    IBlobStorageService blobStorageService,
    IValidator<ModificationAddDocumentDetailsViewModel> documentDetailValidator
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
                    new() { Text = "Select area of change", Value = "" }
                },
                SpecificChangeOptions = new List<SelectListItem>
                {
                    new() { Text = "Select specific change", Value = "" }
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
                .Prepend(new SelectListItem { Value = "", Text = "Select area of change" }),
            SpecificChangeOptions = [new SelectListItem { Value = "", Text = "Select specific change" }]
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
        var tempDataString = TempData.Peek(TempDataKeys.AreaOfChanges) as string;

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

        var modificationId = TempData.Peek(TempDataKeys.ProjectModificationId);
        if (modificationId is Guid newGuid && newGuid != Guid.Empty)
        {
            await SaveModificationChange(model, newGuid);

            TempData[TempDataKeys.AreaOfChangeId] = model.AreaOfChangeId;
            TempData[TempDataKeys.SpecificAreaOfChangeId] = model.SpecificChangeId;
        }

        // Retrieve the journey type from the selected specific change
        var journeyType = GetJourneyTypeFromSession(model);

        // If no journey type is specified, return the AreaOfChange view
        return RedirectBasedOnJourneyType(journeyType, model);
    }

    /// <summary>
    /// Returns the view for selecting a participating organisation.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ParticipatingOrganisation(
        int pageNumber = 1,
        int pageSize = 10,
        List<string>? selectedOrganisationIds = null)
    {
        var viewModel = new SearchOrganisationViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string ?? string.Empty,
            SelectedOrganisationIds = selectedOrganisationIds ?? []
        };

        if (TempData.Peek(TempDataKeys.OrganisationSearchModel) is string json)
        {
            viewModel.Search = JsonSerializer.Deserialize<OrganisationSearchModel>(json)!;
        }

        ServiceResponse<OrganisationSearchResponse> response;
        if (String.IsNullOrEmpty(viewModel.Search.SearchNameTerm))
        {
            response = await rtsService.GetOrganisations(null, pageNumber, pageSize);
        }
        else
        {
            response = await rtsService.GetOrganisationsByName(viewModel.Search.SearchNameTerm, null, pageNumber, pageSize);
        }
        viewModel.Organisations = response?.Content?.Organisations?.Select(dto => new SelectableOrganisationViewModel
        {
            Organisation = new OrganisationModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Address = dto.Address,
                CountryName = dto.CountryName,
                Type = dto.Type
            }
        })
        .ToList() ?? [];

        foreach (var org in viewModel.Organisations)
        {
            if (selectedOrganisationIds?.Contains(org.Organisation.Id) == true)
            {
                org.IsSelected = true;
            }
        }

        viewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, response?.Content?.TotalCount ?? 0)
        {
            SortDirection = SortDirections.Ascending,
            SortField = nameof(OrganisationModel.Name),
            FormName = "organisation-selection"
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
            PageTitle = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string ?? string.Empty,
            CurrentPlannedEndDate = TempData.Peek(TempDataKeys.ProjectPlannedEndDate) as string ?? string.Empty
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
            return View(nameof(SearchOrganisation), model);
        }

        TempData[TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(model.Search);
        return RedirectToAction(nameof(ParticipatingOrganisation));
    }

    /// <summary>
    /// Handles the POST request to save the new planned end date for a project modification.
    /// Validates the input date, builds the modification answers request, and saves the response.
    /// Returns an error view if validation fails or the original response cannot be found.
    /// </summary>
    /// <param name="model">The view model containing the new planned end date.</param>
    /// <returns>Redirects to the project overview on success, or returns the planned end date view with errors.</returns>
    [HttpPost]
    public async Task<IActionResult> SavePlannedEndDate(PlannedEndDateViewModel model)
    {
        // Create a validation context for the new planned end date
        var validationContext = new ValidationContext<DateViewModel>(model.NewPlannedEndDate);

        // Override the property name for validation messages
        validationContext.RootContextData[ValidationKeys.PropertyName] = "NewPlannedEndDate.Date";

        // Validate the date if any date component was provided
        if (model.NewPlannedEndDate.Day is not null ||
            model.NewPlannedEndDate.Month is not null ||
            model.NewPlannedEndDate.Year is not null)
        {
            var validationResult = await dateViewModelValidator.ValidateAsync(validationContext);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                // Return the view with validation errors
                return View(nameof(PlannedEndDate), model);
            }
        }

        // Retrieve previous respondent answers from TempData
        TempData.TryGetValue(TempDataKeys.ProjectRecordResponses, out IEnumerable<RespondentAnswerDto>? projectRecordResponses, true);

        // Get required modification data for saving the planned end date
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModificationChangeId);
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Build the request to save modification answers
        var request = new ProjectModificationAnswersRequest
        {
            ProjectModificationChangeId = projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
            ProjectRecordId = projectRecordId,
            ProjectPersonnelId = respondentId
        };

        // Find the original response for the planned end date question
        var respondedAnswer = projectRecordResponses?.FirstOrDefault(r => r.QuestionId == QuestionIds.ProjectPlannedEndDate);

        if (respondedAnswer == null)
        {
            // Return an error view if the original response cannot be found
            return View("Error", new ProblemDetails
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                Detail = "Unable to save the new planned end date. Couldn't find the original response",
                Status = StatusCodes.Status400BadRequest,
                Instance = Request.Path
            });
        }

        // Add the new planned end date answer to the request
        request.ModificationAnswers.Add(new RespondentAnswerDto
        {
            QuestionId = respondedAnswer.QuestionId,
            VersionId = respondedAnswer.VersionId,
            AnswerText = model.NewPlannedEndDate.Date?.ToString("dd MMMM yyyy"),
            CategoryId = QuestionCategories.ProjectModification,
            SectionId = respondedAnswer.SectionId
        });

        // Save the modification answers using the respondent service
        var saveModificationAnswersResponse = await respondentService.SaveModificationAnswers(request);

        if (!saveModificationAnswersResponse.IsSuccessStatusCode)
        {
            // Return a service error view if saving fails
            return this.ServiceError(saveModificationAnswersResponse);
        }

        return RedirectToAction(nameof(PlannedEndDateOrganisationType));
    }

    /// <summary>
    /// Returns the view to select the organisation types for the planned end date change of the project.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult PlannedEndDateOrganisationType()
    {
        var viewModel = new PlannedEndDateOrganisationTypeViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string ?? string.Empty
        };

        // Redirect to the PlannedEndDateOrganisationType view with the populated view model
        return View(nameof(PlannedEndDateOrganisationType), viewModel);
    }

    /// <summary>
    /// Returns the view to select the organisation types for the planned end date change of the project.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitOrganisationTypes(PlannedEndDateOrganisationTypeViewModel model)
    {
        var validationResult = await organisationTypeValidator.ValidateAsync(new ValidationContext<PlannedEndDateOrganisationTypeViewModel>(model));
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View(nameof(PlannedEndDateOrganisationType), model);
        }

        // Redirect to the project overview on success
        // TODO: Implement next steps for "Save and continue"
        // At the moment both "Save and continue" and "Save for later" redirect to
        // ProjectOverview. Further steps for "Save and continue" will be implemented in next
        // story
        return RedirectToRoute("app:projectoverview");
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
            .Prepend(new SelectListItem { Value = "", Text = "Select area of change" });

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

        // Deserialize the area of changes from TempData
        var areaOfChanges = JsonSerializer.Deserialize<List<GetAreaOfChangesResponse>>(tempDataString)!;
        // Find the specific change based on the selected area and specific change IDs
        var selectedChange = areaOfChanges
            .FirstOrDefault(a => a.Id == model.AreaOfChangeId)?
            .ModificationSpecificAreaOfChanges?
            .FirstOrDefault(sc => sc.Id == model.SpecificChangeId);

        // Store the name of the specific area of change in TempData for later use
        TempData[TempDataKeys.SpecificAreaOfChangeText] = selectedChange?.Name ?? string.Empty;
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