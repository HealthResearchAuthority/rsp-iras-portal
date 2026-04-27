#pragma warning disable S107 // Methods should not have too many parameters

using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Extensions;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications;

[Authorize(Policy = Workspaces.MyResearch)]
[Route("/modifications/[action]", Name = "pmc:[action]")]
public class ReviewAllChangesController
(
    IProjectModificationsService projectModificationsService,
    ICmsQuestionsetService cmsQuestionsetService,
    IRtsService rtsService,
    IRespondentService respondentService,
    IValidator<QuestionnaireViewModel> validator,
    IFeatureManager featureManager,
    IViewHelper viewHelper,
    IValidator<ModificationDetailsViewModel> modificationValidator
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator, featureManager)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";
    private readonly IRespondentService _respondentService = respondentService;

    private readonly ServiceResponse _reviewOutcomeNotFoundError = new()
    {
        StatusCode = HttpStatusCode.NotFound,
        Error = "Unable to retrieve modification review outcome details from session."
    };

    private readonly ServiceResponse _duplicateModifictionError = new()
    {
        StatusCode = HttpStatusCode.NotFound,
        Error = "Unable to duplicate modification from session."
    };

    [Authorize(Policy = Permissions.MyResearch.Modifications_Review)]
    [HttpGet]
    public async Task<IActionResult> ReviewAllChanges
    (
        string projectRecordId,
        string irasId,
        string shortTitle,
        Guid projectModificationId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Ascending,
        bool includeSelectiveDownloadError = false,
        string? backRoute = null
    )
    {
        // Populate TempData with project details for actual modification journey
        TempData[TempDataKeys.IrasId] = irasId;
        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;
        TempData[TempDataKeys.ShortProjectTitle] = shortTitle ?? string.Empty;

        // Fetch the modification by its identifier
        var (result, modification) = await PrepareModificationAsync(projectModificationId, irasId, shortTitle, projectRecordId);

        if (result is not null)
        {
            return result;
        }

        // validate and update the status and answers for the change
        modification.ModificationChanges = await UpdateModificationChanges(projectRecordId, modification.ModificationChanges);

        if (modification.ModificationChanges.Count == 0)
        {
            modification.NoChangesToSubmit = true;
        }
        // Set the 'ready for submission' flag if all changes are ready
        else if (modification.ModificationChanges.All(c => c.ChangeStatus == ModificationStatus.ChangeReadyForSubmission))
        {
            modification.ChangesReadyForSubmission = true;
        }

        var sponsorDetailsQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(SponsorDetailsSectionId);

        // get the responent answers for the sponsor details
        var sponsorDetailsResponse = await _respondentService.GetModificationAnswers(projectModificationId, projectRecordId);

        var sponsorDetailsAnswers = sponsorDetailsResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var sponsorDetailsQuestionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(sponsorDetailsQuestionsResponse.Content!);

        // Apply answers questions using shared helper
        sponsorDetailsQuestionnaire.UpdateWithRespondentAnswers(sponsorDetailsAnswers);

        modification.SponsorDetails = sponsorDetailsQuestionnaire.Questions;

        var modificationAuditResponse = await projectModificationsService.GetModificationAuditTrail(projectModificationId);
        var auditTrailsRecords = modificationAuditResponse.Content?.Items ?? [];
        await SponsorOrganisationNameHelper.GetSponsorOrganisationsNameForAuditRecords(rtsService, auditTrailsRecords);

        if (modificationAuditResponse.IsSuccessStatusCode && modificationAuditResponse.Content is not null)
        {
            modification.AuditTrailModel = new AuditTrailModel
            {
                AuditTrail = modificationAuditResponse.Content,
                ModificationIdentifier = modification.ModificationId ?? "",
                ShortTitle = shortTitle,
            };
        }

        // get all documents for the modification and do pagination here
        var modificationDocumentsResponseResult = await this.GetModificationDocuments(projectModificationId,
            DocumentDetailsSection, 1, int.MaxValue, sortField, sortDirection);

        var allDocuments = modificationDocumentsResponseResult.Item1?.Content?.Documents ?? [];

        await MapDocumentTypesAndStatusesAsync(modificationDocumentsResponseResult.Item2, allDocuments);

        // apply pagination
        var paginatedDocuments = GetSortedAndPaginatedDocuments(allDocuments, sortField, sortDirection, pageSize, pageNumber);

        modification.ProjectOverviewDocumentViewModel.Documents = paginatedDocuments ?? [];

        modification.ProjectOverviewDocumentViewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationDocumentsResponseResult.Item1?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "projectdocuments-selection",
            RouteName = "pmc:reviewallchanges",
            AdditionalParameters = new Dictionary<string, string>()
            {
                { "projectRecordId", projectRecordId },
                { "irasId", irasId },
                { "shortTitle", shortTitle },
                { "projectModificationId", projectModificationId.ToString() }
            }
        };

        // Store the modification details in TempData for later use
        var reviewOutcomeModel = new ReviewOutcomeViewModel
        {
            ModificationDetails = modification,
        };

        TempData[TempDataKeys.ProjectModification.ProjectModificationsDetails] =
            JsonSerializer.Serialize(reviewOutcomeModel);

        // Find all keys where the validation state is Invalid
        var invalidKeys = ModelState
            .Where(ms => ms.Value.ValidationState == ModelValidationState.Invalid)
            .Select(ms => ms.Key)
            .ToList(); // Important: create a list before modifying ModelState

        // Remove each invalid entry from ModelState
        foreach (var key in invalidKeys)
        {
            ModelState.Remove(key);
        }

        var documentsSelectiveDownloadEnabled = await featureManager.IsEnabledAsync(FeatureFlags.DocumentsSelectiveDownload);

        if (documentsSelectiveDownloadEnabled && includeSelectiveDownloadError)
        {
            ModelState.AddModelError("DownloadSelectionButton", "Select at least one document");
        }

        ViewData["BackRoute"] = "pmc:reviewallchanges";
        ViewData[TempDataKeys.ProjectOverviewReferrer.BackRouteValues] = new Dictionary<string, string?>
        {
            { "projectRecordId", projectRecordId },
            { "projectModificationId", projectModificationId.ToString() },
            { "shortTitle", shortTitle },
            { "irasId", irasId }
        };

        // Render the details view
        return View(modification);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpGet]
    public async Task<IActionResult> ReviewOutcome()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        var reviewResponses = await projectModificationsService.GetModificationReviewResponses
        (
            model.ModificationDetails.ProjectRecordId,
            Guid.Parse(model.ModificationDetails.ModificationId!)
        );

        if (reviewResponses.IsSuccessStatusCode && reviewResponses.Content is not null)
        {
            var rfiReasons = reviewResponses.Content.RequestForInformationReasons;

            model.ReviewOutcome = reviewResponses.Content.ReviewOutcome;
            model.Comment = reviewResponses.Content.Comment;
            model.ReasonNotApproved = reviewResponses.Content.ReasonNotApproved;
            model.RequestForInformationReasons = rfiReasons.Count != 0 ? rfiReasons : [string.Empty];
            SaveToTempData(model);
        }

        return View(model);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpPost]
    public async Task<IActionResult> ReviewOutcome(ReviewOutcomeViewModel model, bool saveForLater = false)
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        if (!saveForLater && string.IsNullOrEmpty(model.ReviewOutcome))
        {
            ModelState.AddModelError
            (
                nameof(model.ReviewOutcome),
                "You have not selected an outcome. Select a review outcome before you can continue."
            );

            return View(storedModel);
        }

        storedModel.ReviewOutcome = model.ReviewOutcome;
        storedModel.Comment = model.Comment;

        if (model.ReviewOutcome != ModificationStatus.NotApproved)
        {
            storedModel.ReasonNotApproved = null;
        }

        if (model.ReviewOutcome != ModificationStatus.RequestForInformation)
        {
            storedModel.RequestForInformationReasons = [string.Empty];
        }

        SaveToTempData(storedModel);

        var saveResponsesResponse = await SaveResponses(storedModel);

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        if (saveForLater)
        {
            TempData.Clear();
            TempData[TempDataKeys.ChangeSuccess] = true;
            if (User.IsInRole(Roles.StudyWideReviewer))
            {
                return RedirectToAction("Index", "MyTasklist");
            }

            return RedirectToAction("Index", "ModificationsTasklist");
        }

        if (model.ReviewOutcome == ModificationStatus.NotApproved)
        {
            return RedirectToAction(nameof(ReasonNotApproved));
        }
        else if (model.ReviewOutcome == ModificationStatus.RequestForInformation)
        {
            return RedirectToAction(nameof(RequestForFurtherInformation));
        }

        return RedirectToAction(nameof(ConfirmReviewOutcome));
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpGet]
    public IActionResult ReasonNotApproved()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        return View(model);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpPost]
    public async Task<IActionResult> ReasonNotApproved(ReviewOutcomeViewModel model, bool saveForLater = false)
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        if (!saveForLater && string.IsNullOrEmpty(model.ReasonNotApproved))
        {
            ModelState.AddModelError
            (
                nameof(model.ReviewOutcome),
                "You have not provided a reason. Enter the reason for modification not being approved before you continue."
            );

            return View(storedModel);
        }

        storedModel.ReasonNotApproved = model.ReasonNotApproved;

        return await SaveAndRedirect(storedModel, saveForLater);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpGet]
    public IActionResult RequestForFurtherInformation()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        return View(model);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpPost]
    public async Task<IActionResult> RequestForFurtherInformation(ReviewOutcomeViewModel model, bool saveForLater = false)
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        if (!saveForLater && model.RequestForInformationReasons.All(string.IsNullOrEmpty))
        {
            ModelState.AddModelError
            (
                nameof(model.RequestForInformationReasons) + "[0]",
                "You have not provided a reason. Enter the reason for requesting further information from the applicant before you continue."
            );

            return View(storedModel);
        }

        if (!saveForLater && model.RequestForInformationReasons.Any(r => r != null && r.Length > 300))
        {
            return View(storedModel);
        }

        storedModel.RequestForInformationReasons = [.. model.RequestForInformationReasons.Where(r => !string.IsNullOrEmpty(r))];

        return await SaveAndRedirect(storedModel, saveForLater);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpPost]
    public IActionResult AddRfiReason(ReviewOutcomeViewModel model)
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        model.RequestForInformationReasons.Add(string.Empty);

        storedModel.RequestForInformationReasons = model.RequestForInformationReasons;

        SaveToTempData(storedModel);

        return RedirectToAction(nameof(RequestForFurtherInformation));
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpPost]
    public IActionResult RemoveRfiReason(ReviewOutcomeViewModel model, int index)
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        model.RequestForInformationReasons.RemoveAt(index);

        storedModel.RequestForInformationReasons = model.RequestForInformationReasons;

        SaveToTempData(storedModel);

        return RedirectToAction(nameof(RequestForFurtherInformation));
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpGet]
    public IActionResult ConfirmReviewOutcome()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        return View(model);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpPost]
    public async Task<IActionResult> SubmitReviewOutcome()
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        var saveResponsesResponse = await SaveResponses(storedModel);

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        var modificationId = storedModel.ModificationDetails.ModificationId;

        var newStatus = storedModel.ReviewOutcome;

        var request = new UpdateModificationStatusRequest
        {
            ProjectRecordId = storedModel.ModificationDetails.ProjectRecordId,
            ModificationId = Guid.Parse(modificationId!),
            Status = newStatus!
        };

        var updateResponse = await projectModificationsService.UpdateModificationStatus(request);

        if (!updateResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateResponse);
        }

        return RedirectToAction(nameof(ReviewOutcomeSubmitted));
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Approve)]
    [HttpGet]
    public IActionResult ReviewOutcomeSubmitted()
    {
        return View();
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Submit)]
    public async Task<IActionResult> SendModificationToSponsor(string projectRecordId, Guid projectModificationId, string? applicantRevisionResponse)
    {
        // Fetch all modification documents (up to 200)
        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        searchQuery.AllowedStatuses = User.GetAllowedStatuses(StatusEntitiy.Document);
        var modificationDocumentsResponseResult = await projectModificationsService.GetDocumentsForModification(
            projectModificationId,
            searchQuery, 1, 300,
            nameof(ProjectOverviewDocumentDto.DocumentType),
            SortDirections.Ascending);

        var documents = modificationDocumentsResponseResult?.Content?.Documents ?? [];

        // CHECK FOR INCOMPLETE DOCUMENT DETAILS
        if (documents.Any())
        {
            var documentChangeRequest = BuildDocumentRequest();
            var documentStatuses = await GetDocumentCompletionStatuses(documentChangeRequest);

            bool hasIncompleteDocuments = documentStatuses
                .Any(d => d.Status.Equals(DocumentDetailStatus.Incomplete.ToString(), StringComparison.OrdinalIgnoreCase));

            if (hasIncompleteDocuments)
            {
                return RedirectToRoute("pmc:DocumentDetailsIncomplete");
            }
        }

        // CHECK MALWARE SCAN STATUS
        bool allMalwareScansCompleted = documents.All(d => d.IsMalwareScanSuccessful == true);

        if (!allMalwareScansCompleted)
        {
            return RedirectToRoute("pmc:DocumentsScanInProgress");
        }

        var viewModel = await this.BuildSponsorQuestionnaireViewModel(projectModificationId, projectRecordId, CategoryId);
        var isValid = await this.ValidateQuestionnaire(validator, viewModel, true);

        if (!isValid)
        {
            return View("SponsorReference", viewModel);
        }

        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectModificationId;

        var rfiFeatureFlagEnabled = await featureManager.IsEnabledAsync(FeatureFlags.RequestForInformation);
        ServiceResponse? updateResponse;

        if (rfiFeatureFlagEnabled)
        {
            updateResponse = await projectModificationsService.UpdateModificationStatus(
                new UpdateModificationStatusRequest
                {
                    ProjectRecordId = projectRecordId,
                    ModificationId = projectModificationId,
                    Status = ModificationStatus.WithSponsor,
                    ReasonNotApproved = null,
                    Response = applicantRevisionResponse,
                    Role = ResponseRoles.Applicant,
                    ResponseOrigin = ResponseOrigin.RequestRevisions
                });
        }
        else
        {
            updateResponse = await projectModificationsService.LegacyUpdateModificationStatus
                (
                    projectRecordId,
                    projectModificationId,
                    ModificationStatus.WithSponsor,
                    null,
                    null,
                    applicantRevisionResponse
                );
        }

        if (!updateResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateResponse);
        }

        return View("ModificationSentToSponsor");
    }

    /// <summary>
    /// ModificationSendToSponsor
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = Permissions.MyResearch.Modifications_Submit)]
    [HttpGet]
    public IActionResult ModificationSendToSponsor()
    {
        return View("ModificationSendToSponsor");
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Withdraw)]
    [FeatureGate(RequirementType.Any, FeatureFlags.WithdrawModification, FeatureFlags.RequestForInformation)]
    [HttpGet]
    public IActionResult WithdrawModification()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        return View(model);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Withdraw)]
    [FeatureGate(RequirementType.Any, FeatureFlags.WithdrawModification, FeatureFlags.RequestForInformation)]
    [HttpPost]
    public async Task<IActionResult> ConfirmWithdrawModification(string projectRecordId, Guid projectModificationId)
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectModificationId;

        var updateResponse = await projectModificationsService.UpdateModificationStatus(
            new UpdateModificationStatusRequest
            {
                ProjectRecordId = projectRecordId,
                ModificationId = projectModificationId,
                Status = ModificationStatus.Withdrawn
            });

        if (!updateResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateResponse);
        }

        return View(model);
    }

    public async Task<IActionResult> DownloadModificationPdfFromHtml()
    {
        var modification = GetFromTempData()?.ModificationDetails;

        if (modification is null)
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Error = "Unable to retrieve modification details from session."
            });
        }

        // Fetch all modification documents (up to 100) for the sole purpose of displaying in the PDF
        var modificationDocumentsResponseResult = await this.GetModificationDocuments(
            Guid.Parse(modification.ModificationId!),
            DocumentDetailsSection,
            1,
            100,
            nameof(ProjectOverviewDocumentDto.DocumentType),
            SortDirections.Ascending);

        modification.ProjectOverviewDocumentViewModel.Documents = modificationDocumentsResponseResult.Item1?.Content?.Documents ?? [];

        await MapDocumentTypesAndStatusesAsync(modificationDocumentsResponseResult.Item2, modification.ProjectOverviewDocumentViewModel.Documents);

        var newViewData = new ViewDataDictionary(ViewData)
        {
            [ViewDataKeys.ShowModificationDetails] = true
        };

        var html = await viewHelper.RenderViewAsString(
           "_ReviewModificationPdf",
           modification,
           ControllerContext,
           newViewData);

        var pdf = await viewHelper.GeneratePdf(
           html,
           $"Modification ID: " +
           $"{modification.ModificationIdentifier}");

        var modId = modification.ModificationIdentifier.Split('/');
        var fileName = $"{modId[0]}-{modId[1]}-{DateTime.UtcNow:ddMMMyy}.pdf";

        return File
        (
            pdf,
            "application/pdf",
            fileName
        );
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Resubmission)]
    [FeatureGate(FeatureFlags.ModificationResubmission)]
    [HttpGet]
    public async Task<IActionResult> DuplicateModification()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_duplicateModifictionError);
        }

        var getModificationResponse = await projectModificationsService.GetModification(model.ModificationDetails.ProjectRecordId,
            Guid.Parse(model.ModificationDetails.ModificationId!));

        if (getModificationResponse.IsSuccessStatusCode)
        {
            var currentModification = getModificationResponse.Content;

            // Create a duplicate modification request
            var duplicateModificationRequest = new DuplicateModificationRequest()
            {
                ProjectRecordId = currentModification.ProjectRecordId,
                ExistingModificationId = currentModification.Id
            };

            //Call the service to duplicate the modification
            var duplicateModificationResponse = await projectModificationsService.DuplicateModification(duplicateModificationRequest);

            if (duplicateModificationResponse.IsSuccessStatusCode)
            {
                var duplicateModification = duplicateModificationResponse.Content;

                TempData[TempDataKeys.IsDuplicateModification] = true;
                TempData[TempDataKeys.ShowNotificationBanner] = true;

                return RedirectToAction(nameof(ReviewAllChanges),
                   new
                   {
                       projectRecordId = model.ModificationDetails.ProjectRecordId,
                       irasId = model.ModificationDetails.IrasId,
                       shortTitle = model.ModificationDetails.ShortTitle,
                       projectModificationId = duplicateModification.Id
                   });
            }
        }
        return this.ServiceError(_duplicateModifictionError);
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Review)]
    [FeatureGate(FeatureFlags.RequestRevisions)]
    [HttpPost]
    public async Task<IActionResult> SendRevisionResponseToSponsor(ModificationDetailsViewModel model, bool isSaveForLater)
    {
        bool validation = isSaveForLater && string.IsNullOrWhiteSpace(model.ApplicantRevisionResponse);
        if (!validation)
        {
            var context = new ValidationContext<ModificationDetailsViewModel>(model);
            var validationResult = await modificationValidator.ValidateAsync(context);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                TempData.Remove(TempDataKeys.ModelState);
                TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
                TempData[TempDataKeys.RequestRevisionDescription] = model.ApplicantRevisionResponse;
                return RedirectToRoute("pmc:ModificationDetails", new
                {
                    projectRecordId = model.ProjectRecordId,
                    irasId = model.IrasId,
                    shortTitle = model.ShortTitle,
                    projectModificationId = Guid.Parse(model.ModificationId),
                    sponsorOrganisationUserId = model.SponsorOrganisationUserId,
                    rtsId = model.RtsId
                });
            }
        }

        if (isSaveForLater)
        {
            var rfiFeatureFlagEnabled = await featureManager.IsEnabledAsync(FeatureFlags.RequestForInformation);
            if (rfiFeatureFlagEnabled)
            {
                await projectModificationsService.UpdateModificationStatus(
                    new UpdateModificationStatusRequest
                    {
                        ProjectRecordId = model.ProjectRecordId,
                        ModificationId = Guid.Parse(model.ModificationId),
                        Status = ModificationStatus.RequestRevisions,
                        ReasonNotApproved = model.ReasonNotApproved,
                        Response = model.ApplicantRevisionResponse,
                        Role = ResponseRoles.Applicant,
                        ResponseOrigin = ResponseOrigin.RequestRevisions
                    });
            }
            else
            {
                await projectModificationsService.LegacyUpdateModificationStatus
                    (
                        model.ProjectRecordId,
                        Guid.Parse(model.ModificationId),
                        ModificationStatus.RequestRevisions,
                        model.RevisionDescription,
                        model.ReasonNotApproved,
                        model.ApplicantRevisionResponse
                    );
            }
            TempData[TempDataKeys.ShowNotificationBanner] = true;
            TempData[TempDataKeys.RequestRevisionDescription] = model.ApplicantRevisionResponse;
            return RedirectToRoute("pov:postapproval", new { projectRecordId = model.ProjectRecordId });
        }

        return RedirectToAction(nameof(SendModificationToSponsor), new
        {
            projectRecordId = model.ProjectRecordId,
            projectModificationId = Guid.Parse(model.ModificationId),
            applicantRevisionResponse = model.ApplicantRevisionResponse
        });
    }

    private ReviewOutcomeViewModel? GetFromTempData()
    {
        var serializedModel =
            TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationsDetails) as string;

        if (string.IsNullOrEmpty(serializedModel))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ReviewOutcomeViewModel>(serializedModel);
    }

    private void SaveToTempData(ReviewOutcomeViewModel model)
    {
        TempData[TempDataKeys.ProjectModification.ProjectModificationsDetails] =
            JsonSerializer.Serialize(model);
    }

    private async Task<ServiceResponse> SaveResponses(ReviewOutcomeViewModel model)
    {
        var request = new ProjectModificationReviewRequest
        {
            ProjectModificationId = Guid.Parse(model.ModificationDetails.ModificationId!),
            Outcome = model.ReviewOutcome!,
            Comment = model.Comment,
            ReasonNotApproved = model.ReasonNotApproved,
            RequestForInformationReasons = [.. model.RequestForInformationReasons.Where(r => !string.IsNullOrEmpty(r))]
        };

        SaveToTempData(model);

        return await projectModificationsService.SaveModificationReviewResponses(request);
    }

    private async Task<IActionResult> SaveAndRedirect(ReviewOutcomeViewModel model, bool saveForLater)
    {
        SaveToTempData(model);

        var saveResponsesResponse = await SaveResponses(model);

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        if (saveForLater)
        {
            TempData.Clear();
            TempData[TempDataKeys.ChangeSuccess] = true;
            if (User.IsInRole(Roles.StudyWideReviewer))
            {
                return RedirectToAction("Index", "MyTasklist");
            }

            return RedirectToAction("Index", "ModificationsTasklist");
        }

        return RedirectToAction(nameof(ConfirmReviewOutcome));
    }
}

#pragma warning restore S107 // Methods should not have too many parameters