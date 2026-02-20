#pragma warning disable S107 // Methods should not have too many parameters

using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
using static Rsp.Portal.Application.AccessControl.RoleStatusPermissions;
using static Rsp.Portal.Application.Constants.TempDataKeys;

namespace Rsp.Portal.Web.Features.Modifications;

[Authorize(Policy = Workspaces.MyResearch)]
[Route("/modifications/[action]", Name = "pmc:[action]")]
public class ReviewAllChangesController
(
    IProjectModificationsService projectModificationsService,
    ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService,
    IValidator<QuestionnaireViewModel> validator
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";
    private readonly IRespondentService _respondentService = respondentService;

    private readonly ServiceResponse _reviewOutcomeNotFoundError = new()
    {
        StatusCode = System.Net.HttpStatusCode.NotFound,
        Error = "Unable to retrieve modification review outcome details from session."
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
        string sortDirection = SortDirections.Ascending
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

        if (modificationAuditResponse.IsSuccessStatusCode && modificationAuditResponse.Content is not null)
        {
            modification.AuditTrailModel = new AuditTrailModel
            {
                AuditTrail = modificationAuditResponse.Content,
                ModificationIdentifier = modification.ModificationId ?? "",
                ShortTitle = shortTitle,
            };
        }

        var modificationDocumentsResponseResult = await this.GetModificationDocuments(projectModificationId,
            DocumentDetailsSection, pageNumber, pageSize, sortField, sortDirection);

        modification.ProjectOverviewDocumentViewModel.Documents = modificationDocumentsResponseResult.Item1?.Content?.Documents ?? [];

        await MapDocumentTypesAndStatusesAsync(modificationDocumentsResponseResult.Item2, modification.ProjectOverviewDocumentViewModel.Documents);

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
            model.ReviewOutcome = reviewResponses.Content.ReviewOutcome;
            model.Comment = reviewResponses.Content.Comment;
            model.ReasonNotApproved = reviewResponses.Content.ReasonNotApproved;
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

        if (model.ReviewOutcome == ModificationStatus.Approved)
        {
            storedModel.ReasonNotApproved = null;
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

        return RedirectToAction(nameof(ConfirmReviewOutcome));
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

        var updateResponse = await projectModificationsService.UpdateModificationStatus
        (
            storedModel.ModificationDetails.ProjectRecordId,
            Guid.Parse(modificationId!),
            newStatus!
        );

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
    public async Task<IActionResult> SendModificationToSponsor(string projectRecordId, Guid projectModificationId)
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

        // PASS ALL CHECKS → CONTINUE WORKFLOW
        return await HandleModificationStatusUpdate(
            projectRecordId,
            projectModificationId,
            ModificationStatus.WithSponsor,
            onSuccess: () => View("ModificationSentToSponsor"));
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
    [HttpPost]
    public async Task<IActionResult> ConfirmWithdrawModification(string projectRecordId, Guid projectModificationId)
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        return await HandleModificationStatusUpdate(
            projectRecordId,
            projectModificationId,
            ModificationStatus.Withdrawn,
            onSuccess: () => View(model));
    }


    private async Task<IActionResult> HandleModificationStatusUpdate(
        string projectRecordId,
        Guid projectModificationId,
        string newStatus,
        Func<IActionResult> onSuccess)
    {
        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectModificationId;

        var updateResponse = await projectModificationsService.UpdateModificationStatus
        (
            projectRecordId,
            projectModificationId,
            newStatus
        );

        if (!updateResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateResponse);
        }

        return onSuccess();
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
            ReasonNotApproved = model.ReasonNotApproved
        };

        SaveToTempData(model);

        return await projectModificationsService.SaveModificationReviewResponses(request);
    }
}

#pragma warning restore S107 // Methods should not have too many parameters