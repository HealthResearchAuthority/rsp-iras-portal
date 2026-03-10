using System.Data;
using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Azure;
using Microsoft.FeatureManagement;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Rsp.Portal.Web.Features.Modifications;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Authorize(Policy = Workspaces.MyResearch)]
[Route("[controller]/[action]", Name = "pmc:[action]")]
public class ModificationsController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IBlobStorageService blobStorageService,
    IValidator<AreaOfChangeViewModel> areaofChangeValidator,
    IAzureClientFactory<BlobServiceClient> blobClientFactory,
    IFeatureManager featureManager,
    IApplicationsService applicationsService
) : Controller

{
    private const string StagingContainerName = "staging";
    private const string CleanContainerName = "clean";
    private const string SelectAreaOfChange = "Select area of change";
    private const string SelectSpecificAreaOfChange = "Select specific change";
    private readonly IAzureClientFactory<BlobServiceClient> _blobClientFactory = blobClientFactory;

    /// <summary>
    /// Initiates the creation of a new project modification.
    /// </summary>
    /// <param name="separator">Separator to use in the modification identifier. Default is "/".</param>
    /// <returns>Redirects to the resume route if successful, otherwise returns an error page.</returns>
    [Authorize(Policy = Permissions.MyResearch.Modifications_Create)]
    [HttpGet]
    public async Task<IActionResult> CreateModification(string separator = "/")
    {
        //Restrict new modification creation if there is already in draft modification.
        var canCreateNewModification = TempData[TempDataKeys.ProjectModification.CanCreateNewModification];

        if (canCreateNewModification is false)
        {
            return RedirectToAction(nameof(CreateModificationOutcome));
        }

        // Retrieve IRAS ID from TempData
        var IrasId = TempData.Peek(TempDataKeys.IrasId) as int?;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId);

        // Check if required TempData values are present
        if (string.IsNullOrEmpty((string?)projectRecordId) || IrasId == null)
        {
            // Return a problem response if data is missing

            return this.ServiceError(new ServiceResponse
            {
                Error = "ProjectRecordId and/or IrasId missing",
                StatusCode = HttpStatusCode.BadRequest,
                ReasonPhrase = "Bad Request"
            });
        }

        // Get respondent information from the current context
        var respondent = this.GetRespondentFromContext();

        // Create a new project modification request
        var modificationRequest = new ProjectModificationRequest
        {
            ProjectRecordId = (string)projectRecordId,
            ModificationIdentifier = IrasId + separator,
            Status = ModificationStatus.InDraft,
            CreatedBy = respondent.Id,
            UpdatedBy = respondent.Id
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
        TempData[TempDataKeys.ProjectRecordId] = projectModification.ProjectRecordId;
        return RedirectToAction(nameof(AreaOfChange), new { projectRecordId = projectModification.ProjectRecordId });
    }

    /// <summary>
    /// CreateModificationOutcome
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = Permissions.MyResearch.Modifications_Submit)]
    [HttpGet]
    public IActionResult CreateModificationOutcome()
    {
        return View("CreateModificationOutcome");
    }

    /// <summary>
    /// Displays the Area of Change selection screen.
    /// Retrieves area of change data and stores it in session for later reuse.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Create)]
    [HttpGet]
    public async Task<IActionResult> AreaOfChange(Guid? projectModificationId, string projectRecordId)
    {
        //Get project record status
        (bool okResult, IActionResult errorResult, ServiceResponse<IrasApplicationResponse>? projectRecord) = await GetProjectRecordStatusAsync();
        if (!okResult)
        {
            return errorResult;
        }
        TempData[TempDataKeys.ProjectRecordStatus] = projectRecord?.Content?.Status;
        // if we are adding a new change to the existing modification
        if (projectModificationId.HasValue && projectModificationId != Guid.Empty)
        {
            TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeId);
            TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);

            TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectModificationId;
        }

        // get the initial modification questions from CMS
        var startingQuestionsResponse = await cmsQuestionsetService.GetInitialModificationQuestions();

        if (!startingQuestionsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(startingQuestionsResponse);
        }

        var startingQuestions = startingQuestionsResponse.Content;

        // check if new participating org feature is enabled then show the new option, otherwise old option
        if (await featureManager.IsEnabledAsync(FeatureFlags.ParticipatingOrganisations))
        {
            startingQuestions?.AreasOfChange.RemoveAll(a => a.AutoGeneratedId == AreasOfChange.ParticipatingOrgsWithFreeText);
        }
        else
        {
            startingQuestions?.AreasOfChange.RemoveAll(a => a.AutoGeneratedId == AreasOfChange.ParticipatingOrgsWithSearch);
        }

        var viewModel = new AreaOfChangeViewModel();

        // Handle case when no area of change data is returned from service
        if (startingQuestions == null)
        {
            return View(viewModel);
        }

        // Store the list of area of changes in TempData (as serialized JSON string)
        TempData[TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(startingQuestions.AreasOfChange);

        //Set the modification change marker to an empty GUID only if no change has been applied yet.
        if (TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] is not Guid)
        {
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.Empty;
        }
        if (projectRecord?.Content?.Status == ProjectRecordStatus.ProjectHalt)
        {
            RemoveDropdownOptions(viewModel, startingQuestions.AreasOfChange);
        }
        else
        {
            PopulateDropdownOptions(viewModel, startingQuestions.AreasOfChange);
        }
        TempData.PopulateBaseProjectModificationProperties(viewModel);
        viewModel.SpecificAreaOfChange = SelectAreaOfChange;
        // Populate the dropdown options based on any existing selections
        return View("AreaOfChange", viewModel);
    }

    /// <summary>
    /// For JS disabled - apply selection of selected area of change
    /// </summary>
    /// <param name="areaOfChangeId">Id of selected area of change</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult ApplySelectionToAreaOfChange(string areaOfChangeId)
    {
        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;

        if (string.IsNullOrWhiteSpace(tempDataString))
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "Area of changes not available."
            });
        }

        // Deserialize the area of changes from TempData
        var areaOfChanges = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(tempDataString)!;

        var selectedArea = areaOfChanges?.FirstOrDefault(a => a.AutoGeneratedId == areaOfChangeId);
        if (areaOfChanges is null || selectedArea is null)
        {
            return RedirectToAction("AreaOfChange");
        }

        var viewModel = new AreaOfChangeViewModel();
        viewModel = TempData.PopulateBaseProjectModificationProperties(viewModel);

        viewModel.AreaOfChangeId = areaOfChangeId;
        viewModel.SpecificAreaOfChangeId = Guid.Empty.ToString();

        PopulateDropdownOptions(viewModel, areaOfChanges);

        return View("AreaOfChange", viewModel);
    }

    /// <summary>
    /// Returns the specific changes related to a selected Area of Change.
    /// Pulls data from session cache for performance.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Create)]
    [HttpGet]
    public async Task<IActionResult> GetSpecificChangesByAreaId(string areaOfChangeId)
    {
        //Get project record status from temp data
        (bool okResult, IActionResult errorResult, ServiceResponse<IrasApplicationResponse>? projectRecord) = await GetProjectRecordStatusAsync();
        if (!okResult)
        {
            return errorResult;
        }

        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;
        if (string.IsNullOrWhiteSpace(tempDataString))
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "Area of changes not available."
            });
        }

        // Deserialize the area of changes from TempData
        var areaOfChanges = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(tempDataString)!;

        // Find the specific area of change based on the provided ID
        var selectedArea = areaOfChanges.FirstOrDefault(a => a.AutoGeneratedId == areaOfChangeId);

        // Create a SelectListItem list for the specific changes
        var selectList = new List<SelectListItem> { new() { Value = Guid.Empty.ToString(), Text = SelectSpecificAreaOfChange } };

        //// return only default option
        if (selectedArea == null)
            return Json(selectList);

        // Add each specific change to the SelectListItem list
        if (projectRecord?.Content?.Status == ProjectRecordStatus.ProjectHalt)
        {
            // ONLY allow Project Restart specific item and case-insensitive, trimmed match
            var onlyProjectRestartRequired = selectedArea?.SpecificAreasOfChange
                .Where(sc => !string.IsNullOrWhiteSpace(sc.OptionName) &&
                             sc.OptionName.Trim().Equals(AreasOfChange.ProjectRestart, StringComparison.OrdinalIgnoreCase))
                .Select(sc => new SelectListItem
                {
                    Value = sc.AutoGeneratedId,
                    Text = sc.OptionName
                })
                .ToList() ?? new List<SelectListItem>();

            // Append ONLY the allowed specific item (if present)
            selectList.AddRange(onlyProjectRestartRequired!);
        }
        else
        {
            var specificChanges = selectedArea.SpecificAreasOfChange ?? [];
            selectList.AddRange(specificChanges.Select(sc => new SelectListItem
            {
                Value = sc.AutoGeneratedId,
                Text = sc.OptionName
            }));
        }
        // Return the list as a JSON result
        return Json(selectList);
    }

    /// <summary>
    /// Processes user’s selection of Area and Specific Change and redirects based on journey type.
    /// Saves the modification change to backend and handles validation.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Create)]
    [HttpPost]
    public async Task<IActionResult> ConfirmModificationJourney(AreaOfChangeViewModel model, bool saveForLater = false)
    {
        //Get project record status from temp data
        var projectRecordStatus = TempData[TempDataKeys.ProjectRecordStatus] as string;

        // Deserialize the area of changes from TempData
        var areasJson = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;
        var areaOfChanges = string.IsNullOrWhiteSpace(areasJson)
            ? new List<AreaOfChangeDto>()
            : JsonSerializer.Deserialize<List<AreaOfChangeDto>>(areasJson)!;

        if (!saveForLater)
        {
            PopulateDropdownOptions(model, areaOfChanges);
            var validationResult = await areaofChangeValidator.ValidateAsync(new ValidationContext<AreaOfChangeViewModel>(model));

            if (!validationResult.IsValid)
            {
                HandleValidationErrors(validationResult, model);
                model = TempData.PopulateBaseProjectModificationProperties(model);
                return View(nameof(AreaOfChange), model);
            }
        }
        if (saveForLater)
        {
            TempData[TempDataKeys.ShowNotificationBanner] = true;

            if (model.Status is ModificationStatus.ReviseAndAuthorise)
            {
                return RedirectToRoute("sws:modifications", new { model.SponsorOrganisationUserId, model.RtsId });
            }
            if (projectRecordStatus == ProjectRecordStatus.ProjectHalt)
            {
                RemoveDropdownOptions(model, areaOfChanges);
            }
            return RedirectToRoute("pov:postapproval", new { model.ProjectRecordId });
        }

        // Find the change based on the selected area and specific change IDs
        var areaOfChange = areaOfChanges
            .First(a => a.AutoGeneratedId == model.AreaOfChangeId);

        // Find the specific change based on the selected area and specific change IDs
        var selectedChange = areaOfChange.SpecificAreasOfChange
            .First(sc => sc.AutoGeneratedId == model.SpecificChangeId);
        var modificationId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId);

        TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = selectedChange.OptionName;

        if (modificationId is Guid newGuid && newGuid != Guid.Empty)
        {
            (bool flowControl, IActionResult value) = CheckIsHaltProject(model, selectedChange);
            if (!flowControl)
            {
                return value;
            }
            await SaveModificationChange(model, newGuid);

            TempData[TempDataKeys.ProjectModification.AreaOfChangeId] = model.AreaOfChangeId;
            TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = model.SpecificChangeId;
        }

        // get the modification journey from cms for the
        // sepcific area of change
        var modificationJourney = await cmsQuestionsetService.GetModificationsJourney(selectedChange.AutoGeneratedId!);

        // if we have sections then grab the first section
        var sections = modificationJourney.Content?.Sections ?? [];

        // section shouldn't be null here, this is a defensive
        // check
        if (sections is { Count: 0 } || string.IsNullOrEmpty(sections[0].StaticViewName))
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = $"Unable to load questionnaire for the selected specific area of change: {selectedChange.OptionName}",
            });
        }

        var section = sections[0];

        model.ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId)?.ToString() ?? string.Empty;
        TempData.Keep(TempDataKeys.ProjectRecordStatus);
        return RedirectToRoute($"pmc:{section.StaticViewName}", new
        {
            projectRecordId = model.ProjectRecordId,
            categoryId = section.CategoryId,
            sectionId = section.Id,
        });
    }

    private async Task<(bool flowControl, IActionResult? result, ServiceResponse<IrasApplicationResponse>? record)> GetProjectRecordStatusAsync()
    {
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;
        var record = await applicationsService.GetProjectRecord(projectRecordId);
        if (!record.IsSuccessStatusCode)
        {
            return (flowControl: false, result: this.ServiceError(record), record: record);
        }
        return (flowControl: true, result: null, record: record);
    }

    private (bool flowControl, IActionResult value) CheckIsHaltProject(
     AreaOfChangeViewModel model, AnswerModel selectedChange)
    {
        model.ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty;
        model.IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty;
        model.ModificationId = TempData.PeekGuid(TempDataKeys.ProjectModification.ProjectModificationId);

        // Load previous selections from TempData
        var specificAreaOfChangeOptionNameKey = TempData.Peek(TempDataKeys.SpecificAreaOfChangeOptionNameKey) is string json
            ? JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>()
            : new List<string>();
        bool isHaltNow = selectedChange.OptionName == AreasOfChange.SpecificAreaOfChange;
        bool haltAlreadySelected = specificAreaOfChangeOptionNameKey.Contains(AreasOfChange.SpecificAreaOfChange);
        bool hasOtherChanges = specificAreaOfChangeOptionNameKey.Any(x => x != AreasOfChange.SpecificAreaOfChange);

        // selecting HALT after other changes
        if (isHaltNow && hasOtherChanges && !haltAlreadySelected)
        {
            TempData.Keep(TempDataKeys.SpecificAreaOfChangeOptionNameKey);
            return (false, View("ProjectHaltWarning", model));
        }

        // selecting other changes after HALT
        if (!isHaltNow && haltAlreadySelected)
        {
            TempData.Keep(TempDataKeys.SpecificAreaOfChangeOptionNameKey);
            return (false, View("ProjectHaltWarning", model));
        }

        // Persist the selection
        if (!specificAreaOfChangeOptionNameKey.Contains(selectedChange.OptionName))
            specificAreaOfChangeOptionNameKey.Add(selectedChange.OptionName);

        TempData[TempDataKeys.SpecificAreaOfChangeOptionNameKey] = JsonSerializer.Serialize(specificAreaOfChangeOptionNameKey);

        return (true, null);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Delete)]
    [HttpGet]
    public async Task<IActionResult> DeleteModification(string projectRecordId, string irasId, string shortTitle, Guid projectModificationId)
    {
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
                Error = $"Error retrieving the modification for project record: {projectRecordId} modificationId: {projectModificationId}",
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

        // Render the details view
        return View(viewModel);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteModificationConfirmed
    (
        string projectRecordId,
        Guid projectModificationId,
        string projectModificationIdentifier
    )
    {
        // Call the respondent service to fetch metadata for documents
        var response = await projectModificationsService.GetModificationChanges(projectRecordId, projectModificationId);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        // Get all documents in db
        foreach (var projectModificationChangeResponse in response.Content)
        {
            // Call the respondent service to fetch metadata for documents
            var getModificationChangesDocumentsResponse =
                await respondentService.GetModificationChangesDocuments(
                    projectModificationChangeResponse.Id,
                    projectRecordId
                );

            if (!getModificationChangesDocumentsResponse.IsSuccessStatusCode)
            {
                return this.ServiceError(getModificationChangesDocumentsResponse);
            }

            // Delete all associated documents from blob storage
            foreach (var doc in getModificationChangesDocumentsResponse.Content)
            {
                // Determine whether this document should use the clean container
                bool useClean = doc.IsMalwareScanSuccessful == true;

                // Choose blob client and container based on malware scan result
                var targetBlobClient = GetBlobClient(useClean);
                var targetContainer = useClean ? CleanContainerName : StagingContainerName;

                if (!string.IsNullOrEmpty(doc.DocumentStoragePath))
                {
                    await blobStorageService.DeleteFileAsync(
                        targetBlobClient,
                        containerName: targetContainer,
                        blobPath: doc.DocumentStoragePath
                    );
                }
            }
        }

        // Delete from the DB
        var deleteResponse = await projectModificationsService.DeleteModification(projectRecordId, projectModificationId);

        if (!deleteResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(deleteResponse);
        }

        // Show banner on the next page
        TempData[TempDataKeys.ShowNotificationBanner] = true;
        TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = projectModificationId;

        // Redirect to named route pov:index with both params
        return RedirectToRoute("pov:index", new
        {
            projectRecordId,
            modificationId = projectModificationIdentifier
        });
    }

    /// <summary>
    /// Adds validation errors to ModelState and rebuilds dropdowns from session.
    /// </summary>
    private void HandleValidationErrors(ValidationResult validationResult, AreaOfChangeViewModel model)
    {
        var projectRecordStatus = TempData[TempDataKeys.ProjectRecordStatus] as string;
        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;

        if (!string.IsNullOrWhiteSpace(tempDataString))
        {
            var areaOfChanges = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(tempDataString)!;
            if (projectRecordStatus == ProjectRecordStatus.ProjectHalt)
            {
                RemoveDropdownOptions(model, areaOfChanges);
            }
            else
            {
                PopulateDropdownOptions(model, areaOfChanges);
            }
        }
    }

    /// <summary>
    /// Populates AreaOfChange and SpecificChange dropdowns based on current selection.
    /// </summary>
    private static void PopulateDropdownOptions(AreaOfChangeViewModel model, List<AreaOfChangeDto> areaOfChanges)
    {
        model.AreaOfChangeOptions = areaOfChanges
            .OrderBy(a => a.OptionName)
            .Select(a => new SelectListItem
            {
                Value = a.AutoGeneratedId,
                Text = a.OptionName,
                Selected = a.AutoGeneratedId == model.AreaOfChangeId,
            });

        if (model.AreaOfChangeId != null)
        {
            var selectedArea = areaOfChanges.FirstOrDefault(a => a.AutoGeneratedId == model.AreaOfChangeId);

            if (selectedArea != null)
            {
                model.SpecificChangeOptions = selectedArea.SpecificAreasOfChange
                    .OrderBy(a => a.OptionName)
                    .Select(sc => new SelectListItem
                    {
                        Value = sc.AutoGeneratedId,
                        Text = sc.OptionName,
                        Selected = sc.AutoGeneratedId == model.SpecificChangeId
                    });
            }
        }
    }

    [NonAction]
    public async Task<IActionResult> SaveModificationAnswers(List<RespondentAnswerDto> respondentAnswers, string routeName)
    {
        // Get required modification data for saving the planned end date
        var UserId = (HttpContext.Items[ContextItemKeys.UserId] as string)!;
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Build the request to save modification answers
        var request = new ProjectModificationChangeAnswersRequest
        {
            ProjectModificationChangeId = projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
            ProjectRecordId = projectRecordId,
            UserId = UserId
        };

        // Add the new planned end date answer to the request
        request.ModificationChangeAnswers.AddRange(respondentAnswers);

        // Save the modification answers using the respondent service
        var saveModificationAnswersResponse = await respondentService.SaveModificationChangeAnswers(request);

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
            AreaOfChange = model.AreaOfChangeId!,
            SpecificAreaOfChange = model.SpecificChangeId!,
            ProjectModificationId = modificationId,
            Status = ModificationStatus.InDraft,
            CreatedBy = name,
            UpdatedBy = name
        });

        // Store the modification change ID in TempData for later use
        if (modificationChangeResponse.IsSuccessStatusCode)
        {
            var modificationChange = modificationChangeResponse.Content!;
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChange.Id;
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();
        }
    }

    [HttpGet]
    public async Task<IActionResult> AuditTrail(Guid modificationId, string shortTitle, string modificationIdentifier)
    {
        var auditResponse = await projectModificationsService.GetModificationAuditTrail(modificationId);

        if (!auditResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(auditResponse);
        }

        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId);

        var viewModel = new AuditTrailModel
        {
            AuditTrail = auditResponse.Content!,
            ModificationIdentifier = modificationIdentifier,
            ShortTitle = shortTitle,
            ProjectRecordId = projectRecordId?.ToString()
        };

        return View(viewModel);
    }

    // Example helper method to get correct blob client
    private BlobServiceClient GetBlobClient(bool useCleanContainer)
    {
        var name = useCleanContainer ? "Clean" : "Staging";
        return _blobClientFactory.CreateClient(name);
    }

    private static void RemoveDropdownOptions(AreaOfChangeViewModel model, List<AreaOfChangeDto> areaOfChanges)
    {
        if (model == null || areaOfChanges == null)
            return;

        //Limit AreaOfChangeOptions to only "Stop or restart"
        var allowedAreas = areaOfChanges
            .Where(a => string.Equals(a.OptionName?.Trim(), AreasOfChange.AllowedAreaName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.OptionName)
            .ToList();

        model.AreaOfChangeOptions = allowedAreas
            .Select(a => new SelectListItem
            {
                Value = a.AutoGeneratedId,
                Text = a.OptionName,
                Selected = a.AutoGeneratedId == model.AreaOfChangeId
            });
    }
}