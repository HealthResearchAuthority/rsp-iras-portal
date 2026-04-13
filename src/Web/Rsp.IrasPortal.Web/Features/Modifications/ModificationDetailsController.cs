using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications;

[Authorize(Policy = Workspaces.MyResearch)]
[Route("/modifications/[action]", Name = "pmc:[action]")]
public class ModificationDetailsController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IModificationRankingService modificationRankingService,
    ISponsorOrganisationService sponsorOrganisationService,
    ISponsorUserAuthorisationService sponsorUserAuthorisationService,
    IValidator<QuestionnaireViewModel> validator,
    IFeatureManager featureManager
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator, featureManager)
{
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private const string DocumentDetailsSection = "pdm-document-metadata";

    /// <summary>
    /// Displays the modification details page for a given project modification.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier of the project record that owns the modification.</param>
    /// <param name="irasId">The IRAS identifier for display in the page header.</param>
    /// <param name="shortTitle">The project's short title for display in the page header.</param>
    /// <param name="projectModificationId">The unique identifier of the project modification to display.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> that renders the details view when successful; otherwise,
    /// an error result produced by <c>this.ServiceError(...)</c> when a service call fails.
    /// </returns>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Read)]
    [HttpGet]
    public async Task<IActionResult> ModificationDetails
    (
        string projectRecordId,
        string irasId,
        string shortTitle,
        Guid projectModificationId,
        Guid? sponsorOrganisationUserId = null,
        string? rtsId = null,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Ascending,
        bool includeSelectiveDownloadError = false
    )
    {
        // all changes are being reviewing here so remove the change specific keys from tempdata
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChanges);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeText);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeText);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeMarker);

        TempData[TempDataKeys.IrasId] = irasId;
        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;
        TempData[TempDataKeys.ShortProjectTitle] = shortTitle;

        var (result, modification) = await PrepareModificationAsync(projectModificationId, irasId, shortTitle, projectRecordId);
        if (result is not null)
        {
            return result;
        }

        if (modification is null)
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = $"There is no modification for modification id {projectModificationId}",
            });
        }
        // If its sponsor revision - validate if sponsor is authoriser
        if ((modification.Status is ModificationStatus.ReviseAndAuthorise && sponsorOrganisationUserId != null && rtsId != null) ||
            (modification.Status is ModificationStatus.RequestRevisions))
        {
            // Add sponsor details
            modification.SponsorOrganisationUserId = sponsorOrganisationUserId?.ToString();
            modification.RtsId = rtsId;

            var revisionDescription = TempData[TempDataKeys.RevisionDescription];
            if (!string.IsNullOrEmpty(revisionDescription?.ToString()))
            {
                modification.RevisionDescription = revisionDescription.ToString();
            }
            var requestRevisionDescription = TempData[TempDataKeys.RequestRevisionDescription];
            if (!string.IsNullOrEmpty(requestRevisionDescription?.ToString()))
            {
                modification.ApplicantRevisionResponse = requestRevisionDescription.ToString();
                TempData.Keep(TempDataKeys.RequestRevisionDescription);
            }

            var sponsorDetailsQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(SponsorDetailsSectionId);

            // get the responent answers for the sponsor details
            var sponsorDetailsResponse = await respondentService.GetModificationAnswers(projectModificationId, projectRecordId);

            var sponsorDetailsAnswers = sponsorDetailsResponse.Content!;

            // convert the questions response to QuestionnaireViewModel
            var sponsorDetailsQuestionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(sponsorDetailsQuestionsResponse.Content!);

            // Apply answers questions using shared helper
            sponsorDetailsQuestionnaire.UpdateWithRespondentAnswers(sponsorDetailsAnswers);

            modification.SponsorDetails = sponsorDetailsQuestionnaire.Questions;

            // Add modification documents
            var modificationDocumentsResponseResult = await this.GetModificationDocuments(projectModificationId,
            DocumentDetailsSection, 1, int.MaxValue, sortField, sortDirection, isSponsorRevisingModification: true);

            var allDocuments = modificationDocumentsResponseResult.Item1?.Content?.Documents ?? [];

            await MapDocumentTypesAndStatusesAsync(modificationDocumentsResponseResult.Item2, allDocuments, false, showIncompleteForReviseAndAuthoriseStatus: true);

            // apply pagination
            var paginatedDocuments = GetSortedAndPaginatedDocuments(allDocuments, sortField, sortDirection, pageSize, pageNumber);

            modification.ProjectOverviewDocumentViewModel.Documents = paginatedDocuments;

            modification.ProjectOverviewDocumentViewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationDocumentsResponseResult.Item1?.Content?.TotalCount ?? 0)
            {
                SortDirection = sortDirection,
                SortField = sortField,
                FormName = "projectdocuments-selection",
                RouteName = "pmc:modificationdetails",
                AdditionalParameters = new Dictionary<string, string>()
                {
                    { "projectRecordId", projectRecordId },
                    { "irasId", irasId },
                    { "shortTitle", shortTitle },
                    { "projectModificationId", projectModificationId.ToString() },
                    { "sponsorOrganisationUserId", sponsorOrganisationUserId.ToString()?? string.Empty },
                    { "rtsId", rtsId ?? string.Empty }
                }
            };
        }

        // validate and update the status and answers for the change
        modification.ModificationChanges = await UpdateModificationChanges(projectRecordId, modification.ModificationChanges);
        // Set the 'ready for submission' flag if all changes are ready
        if (modification.ModificationChanges.All(c => c.ChangeStatus == ModificationStatus.ChangeReadyForSubmission))
        {
            modification.ChangesReadyForSubmission = true;
        }
        if (modification.ModificationChanges.Count == 0)
        {
            modification.NoChangesToSubmit = true;
        }

        var documentsSelectiveDownloadEnabled = await featureManager.IsEnabledAsync(FeatureFlags.DocumentsSelectiveDownload);

        if (documentsSelectiveDownloadEnabled && includeSelectiveDownloadError)
        {
            ModelState.AddModelError("DownloadSelectionButton", "Select at least one document");
        }

        // Render the details view
        return View(modification);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Read)]
    [HttpGet]
    public IActionResult UnfinishedChanges()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        return View("UnfinishedChanges", viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Read)]
    [HttpGet]
    public IActionResult NoChangesToSubmit()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        return View("NoChangesToSubmit", viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Read)]
    [HttpGet]
    public IActionResult DocumentsScanInProgress()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        return View("DocumentsScanInProgress", viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Read)]
    [HttpGet]
    public IActionResult DocumentDetailsIncomplete()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        return View("DocumentDetailsIncomplete", viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Delete)]
    [HttpGet]
    public IActionResult ConfirmRemoveChange(string modificationChangeId, string modificationChangeName)
    {
        //this temp data is used for project halt validation
        TempData.Remove(TempDataKeys.SpecificAreaOfChangeOptionNameKey);
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsViewModel());

        viewModel.Status = (viewModel as BaseProjectModificationViewModel).Status;
        viewModel.ModificationChangeId = modificationChangeId;
        viewModel.SpecificAreaOfChange = modificationChangeName;

        return View(viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveChange(Guid modificationChangeId, string modificationChangeName)
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsViewModel());

        var removeChangeResponse = await projectModificationsService.RemoveModificationChange(modificationChangeId);

        if (!removeChangeResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(removeChangeResponse);
        }

        // update the overall ranking after removing the change
        await modificationRankingService.UpdateOverallRanking(Guid.Parse(viewModel.ModificationId!), viewModel.ProjectRecordId);

        TempData[TempDataKeys.ProjectModificationChange.ChangeRemoved] = true;
        TempData[TempDataKeys.ProjectModificationChange.ChangeName] = modificationChangeName;

        return RedirectToAction(nameof(ModificationDetails), new
        {
            projectRecordId = viewModel.ProjectRecordId,
            irasId = viewModel.IrasId,
            shortTitle = viewModel.ShortTitle,
            projectModificationId = viewModel.ModificationId,
            sponsorOrganisationUserId = viewModel.SponsorOrganisationUserId,
            rtsId = viewModel.RtsId
        });
    }

    [Authorize(Policy = Permissions.MyResearch.Modifications_Read)]
    [FeatureGate(FeatureFlags.RequestRevisions)]
    [HttpGet]
    public async Task<IActionResult> RequestForRevision(string projectRecordId,
    string irasId,
    string shortTitle,
    Guid projectModificationId)
    {
        var modification = await projectModificationsService.GetModification(projectRecordId, projectModificationId);

        if (!modification.IsSuccessStatusCode)
        {
            return this.ServiceError(modification);
        }
        if (modification.Content is null)
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = $"There is no modification for modification id {projectModificationId}",
            });
        }

        var model = new ModificationDetailsViewModel
        {
            IrasId = irasId,
            ShortTitle = shortTitle,
            DateSponsorSubmittedOutcome = DateHelper.ConvertDateToString(modification.Content.DateSponsorSubmittedOutcome),
            RevisionDescription = modification.Content.RevisionDescription,
            ProjectRecordId = projectRecordId,
            ModificationId = projectModificationId.ToString(),
            Status = modification.Content.Status,
            ModificationIdentifier = modification.Content.ModificationIdentifier
        };
        TempData[TempDataKeys.RevisionDescription] = modification.Content.RevisionDescription;
        return View("MakeRevisonByApplicant", model);
    }
}