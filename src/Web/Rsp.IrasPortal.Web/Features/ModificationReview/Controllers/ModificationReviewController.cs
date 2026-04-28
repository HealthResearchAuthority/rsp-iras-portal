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
using Rsp.IrasPortal.Application.Enum;
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
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Rsp.Portal.Web.Features.Modifications;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Authorize(Policy = Workspaces.MyResearch)]
[Route("[controller]/[action]", Name = "mr:[action]")]
public class ModificationReviewController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    IRtsService rtsService,
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

    [Authorize(Policy = Permissions.MyResearch.Modifications_Delete)]
    [HttpGet]
    public async Task<IActionResult> DeleteModification(string projectRecordId, string irasId, string shortTitle, Guid projectModificationId)
    {
        //this temp data is used for project halt validation
        TempData.Remove(TempDataKeys.SpecificAreaOfChangeOptionNameKey);

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
    private async Task HandleValidationErrors(ValidationResult validationResult, AreaOfChangeViewModel model)
    {
        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;

        if (!string.IsNullOrWhiteSpace(tempDataString))
        {
            var areaOfChanges = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(tempDataString)!;
            await PopulateDropdownOptions(model, areaOfChanges);
        }
    }

    /// <summary>
    /// Populates AreaOfChange and SpecificChange dropdowns based on current selection.
    /// </summary>
    private async Task PopulateDropdownOptions(AreaOfChangeViewModel model, List<AreaOfChangeDto> areaOfChanges)
    {
        //Get project record status
        (bool okResult, ServiceResponse<IrasApplicationResponse>? projectRecord) = await GetProjectRecordStatusAsync();
        if (!okResult)
        {
            return;
        }
        if (projectRecord?.Content?.Status == ProjectRecordStatus.ProjectHalt)
        {
            RemoveDropdownOptions(model, areaOfChanges);
            if (model.AreaOfChangeId != null)
            {
                var selectedArea = areaOfChanges.FirstOrDefault(a => a.AutoGeneratedId == model.AreaOfChangeId);

                if (selectedArea != null)
                {
                    // ONLY allow Project Restart specific item and case-insensitive, trimmed match
                    var onlyProjectRestartRequired = selectedArea?.SpecificAreasOfChange
                    .Where(sc => !string.IsNullOrWhiteSpace(sc.OptionName) &&
                                 sc.AutoGeneratedId.Trim().Equals(AreasOfChange.ProjectRestart, StringComparison.OrdinalIgnoreCase))
                    .Select(sc => new SelectListItem
                    {
                        Value = sc.AutoGeneratedId,
                        Text = sc.OptionName
                    })
                    .ToList();

                    // Append ONLY the allowed specific item
                    model.SpecificChangeOptions = onlyProjectRestartRequired!;
                }
            }
        }
        else
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

        var auditTrailsRecords = auditResponse.Content?.Items ?? [];
        await SponsorOrganisationNameHelper.GetSponsorOrganisationsNameForAuditRecords(rtsService, auditTrailsRecords);

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
            .Where(a => string.Equals(a.AutoGeneratedId?.Trim(), AreasOfChange.AllowedAreaName, StringComparison.OrdinalIgnoreCase))
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

    private async Task<(bool flowControl, ServiceResponse<IrasApplicationResponse>? record)> GetProjectRecordStatusAsync()
    {
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;
        var record = await applicationsService.GetProjectRecord(projectRecordId!);
        if (!record.IsSuccessStatusCode)
        {
            return (flowControl: false, record: record);
        }
        return (flowControl: true, record: record);
    }

    private async Task<(bool flowControl, IActionResult value)> CheckIfSelectedChangeIsAllowed(
    AreaOfChangeViewModel model,
    AnswerModel selectedChange)
    {
        // Populate model from TempData (safe peeks)
        model.ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty;
        model.IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty;
        model.ModificationId = TempData.PeekGuid(TempDataKeys.ProjectModification.ProjectModificationId);

        // Load previously selected areas from tempData if not database
        var previousSelections = await GetPreviousSelectionsAsync(model);

        // Compute the decision
        var validation = EvaluateSpecificAreaOfChange(previousSelections, selectedChange);
        if (!validation.IsValid)
        {
            return (false, View("SelectedModificationChangeErrorPage", (model, validation.InvalidSpecificAreaOfChangeId)));
        }

        // Persist current selection into TempData for continuity in the current session
        if (!previousSelections.Contains(selectedChange.AutoGeneratedId))
        {
            previousSelections.Add(selectedChange.AutoGeneratedId);
        }

        TempData[TempDataKeys.SpecificAreaOfChangeOptionNameKey] =
            JsonSerializer.Serialize(previousSelections);

        return (true, null);
    }

    /// <summary>
    /// Returns prior selections using TempData if present (applicant journey),
    /// otherwise loads from the database (sponsor journey).
    /// Always returns a non-null list.
    /// </summary>
    private async Task<List<string>> GetPreviousSelectionsAsync(AreaOfChangeViewModel model)
    {
        var result = new List<string>();
        if (TempData.Peek(TempDataKeys.SpecificAreaOfChangeOptionNameKey) is string json &&
            !string.IsNullOrWhiteSpace(json))
        {
            result = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            // Keep the key alive for subsequent steps in the same session
            TempData.Keep(TempDataKeys.SpecificAreaOfChangeOptionNameKey);
        }

        if (result.Count > 0)
        {
            // Applicant flow: TempData already carries state across the wizard
            return result;
        }

        // Sponsor flow: read persisted changes from DB
        return await BuildSelectionsFromDatabaseAsync(model);
    }

    /// <summary>
    /// Encapsulates all selected change validation rules
    /// </summary>
    private (bool IsValid, string? InvalidSpecificAreaOfChangeId) EvaluateSpecificAreaOfChange(
        IReadOnlyCollection<string> previousSelections,
        AnswerModel selectedChange)
    {
        var specificChangesWithNoOtherChangesRules = new[]
        {
            AreasOfChange.ProjectHalt,
            AreasOfChange.ProjectRestart,
            AreasOfChange.ChangeOfPrimarySponsor
        };

        var isCurrentSpecificChangeWithNoOtherChangesRules = specificChangesWithNoOtherChangesRules.Contains(selectedChange?.AutoGeneratedId);
        var existingSpecificChangesWithNoOtherChangesRules = previousSelections.FirstOrDefault(id => specificChangesWithNoOtherChangesRules.Contains(id));
        var hasOtherChanges = previousSelections.Any();

        // conflict: now selecting no-other while other exist
        if (isCurrentSpecificChangeWithNoOtherChangesRules && hasOtherChanges)
        {
            return (false, selectedChange?.AutoGeneratedId);
        }

        // conflict: selecting any change when already have no-other
        if (existingSpecificChangesWithNoOtherChangesRules != null)
        {
            return (false, existingSpecificChangesWithNoOtherChangesRules);
        }

        return (true, null);
    }

    private async Task<List<string>> BuildSelectionsFromDatabaseAsync(AreaOfChangeViewModel model)
    {
        var result = new List<string>();

        // Ensure ModificationId is a valid Guid
        if (!Guid.TryParse(model.ModificationId, out var modificationId))
        {
            return result;
        }

        // If ProjectRecordId is required, ensure it is present/valid upstream.
        var response = await projectModificationsService
            .GetModificationChanges(model.ProjectRecordId, modificationId);

        if (response?.Content == null)
            return result;

        // Assuming SpecificAreaOfChange is the AutoGeneratedId / string key you compare against
        foreach (var item in response.Content)
        {
            if (!string.IsNullOrWhiteSpace(item?.SpecificAreaOfChange))
            {
                result.Add(item.SpecificAreaOfChange);
            }
        }

        return result;
    }
}