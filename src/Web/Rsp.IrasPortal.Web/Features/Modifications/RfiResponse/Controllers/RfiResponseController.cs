using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Controllers;

[FeatureGate(FeatureFlags.RequestForInformation)]
[Authorize(Policy = Workspaces.MyResearch)]
[Route("/modifications/rfi/[action]", Name = "rfi:[action]")]
public class RfiResponseController(
    IProjectModificationsService projectModificationsService,
    IApplicationsService projectRecordService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IModificationRankingService modificationRankingService,
    ISponsorOrganisationService sponsorOrganisationService,
    ISponsorUserAuthorisationService sponsorUserAuthorisationService,
    IValidator<QuestionnaireViewModel> validator,
    IFeatureManager featureManager
    ) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator, featureManager)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";

    [HttpGet]
    public async Task<IActionResult> RfiDetails(string projectId, Guid modificationId)
    {
        var model = new RfiDetailsViewModel();

        // get modification details
        var modification = await projectModificationsService.GetModification(projectId, modificationId);
        var projectRecord = await projectRecordService.GetProjectRecord(projectId);

        var rfiReasons = await projectModificationsService.GetModificationReviewResponses(projectId, modificationId);
        var rfiResponses = await projectModificationsService.GetModificationRfiResponses(projectId, modificationId);

        if (!modification.IsSuccessStatusCode ||
            modification.Content == null ||
            !projectRecord.IsSuccessStatusCode ||
            projectRecord.Content == null ||
            !rfiReasons.IsSuccessStatusCode ||
            !rfiResponses.IsSuccessStatusCode)
        {
            this.ServiceError(modification);
        }

        // only allow access to RFI details page when the modification is in RFI status,
        // otherwise return 403 forbidden
        if (modification.Content!.Status != ModificationStatus.RequestForInformation)
        {
            return Forbid();
        }

        model.IrasId = projectRecord.Content!.IrasId.ToString();
        model.ModificationIdentifier = modification.Content.ModificationIdentifier;
        model.ShortTitle = projectRecord.Content.ShortProjectTitle;
        model.DateSubmitted = modification.Content.SentToRegulatorDate?.ToString("dd MMMM yyyy");
        model.RfiReasons = rfiReasons.Content == null ? [] : rfiReasons.Content.RequestForInformationReasons;
        model.RfiResponses = rfiResponses.Content == null ? [] : rfiResponses.Content.RfiResponses;
        model.ProjectRecordId = projectId;
        model.ModificationId = modificationId.ToString();
        model.Status = modification.Content.Status;

        while (model.RfiResponses.Count < model.RfiReasons.Count)
        {
            // We now use a response object instead of a single string because:
            // - each RFI reason can have multiple responses (1:N relationship)
            // - responses can belong to different stages (see ResponseOrigin constant)
            // used in SaveModificationRfiResponses method
            var rfiResponse = new RfiResponsesDTO();

            // Pre-populate InitialResponse with an empty value to preserve
            // existing binding behaviour (previously this was a single string).
            rfiResponse.InitialResponse.Add(string.Empty);

            model.RfiResponses.Add(rfiResponse);
        }

        TempData[TempDataKeys.RfiDetails] = JsonSerializer.Serialize(model);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> RfiResponses
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
        var jsonModel = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;

        var model = JsonSerializer.Deserialize<RfiDetailsViewModel>(jsonModel)!;

        // Add modification documents
        var modificationDocumentsResponseResult = await this.GetModificationDocuments(Guid.Parse(model.ModificationId),
        DocumentDetailsSection, 1, int.MaxValue, sortField, sortDirection, isSponsorRevisingModification: true);

        var allDocuments = modificationDocumentsResponseResult.Item1?.Content?.Documents ?? [];

        await MapDocumentTypesAndStatusesAsync(modificationDocumentsResponseResult.Item2, allDocuments, false, showIncompleteForReviseAndAuthoriseStatus: true);

        // apply pagination
        var paginatedDocuments = GetSortedAndPaginatedDocuments(allDocuments, sortField, sortDirection, pageSize, pageNumber);

        model.ProjectOverviewDocumentViewModel.Documents = paginatedDocuments;

        model.ProjectOverviewDocumentViewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationDocumentsResponseResult.Item1?.Content?.TotalCount ?? 0)
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

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RfiResponses(RfiDetailsViewModel model, bool saveForLater = false)
    {
        var storedModelJson = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;
        var storedModel = JsonSerializer.Deserialize<RfiDetailsViewModel>(storedModelJson)!;

        var responses = model.RfiResponses
                .Select(r => r.InitialResponse.FirstOrDefault() ?? string.Empty)
                .ToList();

        storedModel.RfiResponses = model.RfiResponses;

        if (!saveForLater && responses.Any(string.IsNullOrWhiteSpace))
        {
            ModelState.AddModelError(string.Empty, "You have not provided a reason. Enter the reason for requesting further information from the applicant before you continue.");
            return View(storedModel);
        }

        var saveResponsesResponse = await projectModificationsService.SaveModificationRfiResponses(
            new ModificationRfiResponseRequest()
            {
                ProjectModificationId = Guid.Parse(storedModel.ModificationId!),
                Responses = responses,
                Role = ResponseRoles.Applicant,
                ResponseOrigin = ResponseOrigin.InitialResponse
            }
        );

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        if (saveForLater)
        {
            TempData.Remove(TempDataKeys.RfiDetails);
            return RedirectToAction(
                "ReviewAllChanges",
                "ReviewAllChanges",
                new
                {
                    projectRecordId = storedModel.ProjectRecordId,
                    irasId = storedModel.IrasId,
                    shortTitle = storedModel.ShortTitle,
                    projectModificationId = Guid.Parse(storedModel.ModificationId!),
                }
            );
        }

        TempData[TempDataKeys.RfiDetails] = JsonSerializer.Serialize(storedModel);

        return RedirectToAction(nameof(RfiCheckAndSubmitResponses));
    }

    [HttpGet]
    public IActionResult RfiCheckAndSubmitResponses()
    {
        var jsonModel = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;

        var model = JsonSerializer.Deserialize<RfiDetailsViewModel>(jsonModel)!;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RfiSubmitResponses()
    {
        var storedModelJson = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;
        var storedModel = JsonSerializer.Deserialize<RfiDetailsViewModel>(storedModelJson)!;

        var updateStatusResponse = await projectModificationsService.UpdateModificationStatus(
            new UpdateModificationStatusRequest
            {
                ProjectRecordId = storedModel.ProjectRecordId!,
                ModificationId = Guid.Parse(storedModel.ModificationId!),
                Status = ModificationStatus.ResponseWithSponsor
            });

        if (!updateStatusResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateStatusResponse);
        }

        return RedirectToAction(nameof(RfiResponsesConfirmation));
    }

    [HttpGet]
    public IActionResult RfiResponsesConfirmation()
    {
        return View();
    }
}