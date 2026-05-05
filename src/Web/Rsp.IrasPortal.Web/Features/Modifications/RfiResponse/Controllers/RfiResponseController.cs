using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Extensions;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Helpers;
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
    IValidator<RfiResponsesDTO> rfiResponsesValidator,
    IFeatureManager featureManager
    ) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator, featureManager)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";

    [HttpGet]
    [Authorize(Policy = Permissions.MyResearch.Modifications_Read)]
    public async Task<IActionResult> RfiDetails(string projectId, Guid modificationId)
    {
        var model = new ModificationDetailsViewModel();

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
        if (modification.Content!.Status is not ModificationStatus.RequestForInformation and not ModificationStatus.ResponseRequestRevisions)
        {
            return Forbid();
        }

        model.IrasId = projectRecord.Content!.IrasId.ToString();
        model.ModificationIdentifier = modification.Content.ModificationIdentifier;
        model.ShortTitle = projectRecord.Content.ShortProjectTitle;
        model.RfiModel = new RfiDetailsViewModel
        {
            RfiReasons = rfiReasons.Content.RequestForInformationReasons ?? [],
            RfiResponses = rfiResponses.Content?.RfiResponses ?? [],
        };
        model.ProjectRecordId = projectId;
        model.ModificationId = modificationId.ToString();
        model.Status = modification.Content.Status;
        model.DateSubmitted = DateHelper.ConvertDateToString(modification?.Content.SentToRegulatorDate);

        while (model.RfiModel.RfiResponses.Count < model.RfiModel.RfiReasons.Count)
        {
            // We now use a response object instead of a single string because:
            // - each RFI reason can have multiple responses (1:N relationship)
            // - responses can belong to different stages (see ResponseOrigin constant)
            // used in SaveModificationRfiResponses method
            var rfiResponse = new RfiResponsesDTO();

            // Pre-populate InitialResponse with an empty value to preserve
            // existing binding behaviour (previously this was a single string).
            rfiResponse.InitialResponse.Add(string.Empty);

            model.RfiModel.RfiResponses.Add(rfiResponse);
        }
        return View(model);
    }

    [HttpGet]
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Review)]
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
        TempData[TempDataKeys.IrasId] = irasId;
        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;
        TempData[TempDataKeys.ShortProjectTitle] = shortTitle;

        // Retrieve all modification changes related to the modification
        var (result, model) = await PrepareModificationAsync(projectModificationId, irasId, shortTitle, projectRecordId);
        if (result is not null)
        {
            return result;
        }
        model.RtsId = rtsId;
        model.SponsorOrganisationUserId = sponsorOrganisationUserId.ToString();

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
        model.ModificationChanges = model!.ModificationChanges.ToList();

        // get back answers user set in SendRfiResponses for validation errors
        if (TempData.TryGetValue(TempDataKeys.RfiResponses, out var raw))
        {
            MergeRfiResponseDataFromTempData(model, raw as string);
        }
        //For PDF download data population
        var reviewOutcomeModel = new ReviewOutcomeViewModel
        {
            ModificationDetails = model,
        };

        TempData[TempDataKeys.ProjectModification.ProjectModificationsDetails] =
            JsonSerializer.Serialize(reviewOutcomeModel);

        return View(model);
    }

    internal static void MergeRfiResponseDataFromTempData(ModificationDetailsViewModel model, string responsesJson)
    {
        if (string.IsNullOrWhiteSpace(responsesJson))
        {
            return;
        }

        var tempResponses =
            JsonSerializer.Deserialize<List<RfiResponsesDTO>>(responsesJson);

        if (tempResponses is null)
        {
            return;
        }

        model.RfiModel.RfiResponses
            .Zip(tempResponses, (target, temp) => new { target, temp })
            .ToList()
            .ForEach(pair =>
            {
                CopyDependingOnStatus(
                              pair.temp.InitialResponse,
                              pair.target.InitialResponse,
                              model.Status == ModificationStatus.RequestForInformation);
                CopyDependingOnStatus(
                               pair.temp.RequestRevisionsByApplicant,
                               pair.target.RequestRevisionsByApplicant,
                               model.Status == ModificationStatus.ResponseRequestRevisions);
                CopyDependingOnStatus(
                               pair.temp.RequestRevisionsBySponsor,
                               pair.target.RequestRevisionsBySponsor,
                               model.Status == ModificationStatus.ResponseWithSponsor);
                CopyDependingOnStatus(
                               pair.temp.ReviseAndAuthorise,
                               pair.target.ReviseAndAuthorise,
                               model.Status == ModificationStatus.ResponseReviseAndAuthorise);
                CopyDependingOnStatus(
                              pair.temp.ReasonForReviseAndAuthorise,
                              pair.target.ReasonForReviseAndAuthorise,
                              model.Status == ModificationStatus.ResponseReviseAndAuthorise);
            });
    }

    private static void CopyDependingOnStatus(
        IList<string> source,
        IList<string> target,
        bool allowOverwriteWithEmpty)
    {
        if (source.Count == 0)
            return;

        var value = source[0];

        if (!allowOverwriteWithEmpty &&
            string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        target[0] = value ?? string.Empty;
    }

    [HttpPost]
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Review)]
    public async Task<IActionResult> SendRfiResponses(ModificationDetailsViewModel model, bool saveForLater = false)
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(model);
        viewModel.Status = (viewModel as BaseProjectModificationViewModel).Status;

        if (!saveForLater)
        {
            for (int i = 0; i < viewModel.RfiModel.RfiResponses.Count; i++)
            {
                var response = viewModel.RfiModel.RfiResponses[i];

                var context = new ValidationContext<RfiResponsesDTO>(response);
                var validationResult = await rfiResponsesValidator.ValidateAsync(context);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        ModelState.AddModelError(
                            $"RfiModel_RfiResponses_{i}__{error.PropertyName}_",
                            error.ErrorMessage);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                TempData.Remove(TempDataKeys.ModelState);
                TempData.TryAdd(
                    TempDataKeys.ModelState,
                    ModelState.ToDictionary(),
                    true);

                // Save responses temporarly to not lost them during redirect to action
                TempData[TempDataKeys.RfiResponses] =
                    JsonSerializer.Serialize(viewModel.RfiModel.RfiResponses);

                return RedirectToAction(nameof(RfiResponses), new
                {
                    projectRecordId = viewModel.ProjectRecordId,
                    irasId = viewModel.IrasId,
                    shortTitle = viewModel.ShortTitle,
                    projectModificationId = Guid.Parse(viewModel.ModificationId!),
                    sponsorOrganisationUserId = viewModel.SponsorOrganisationUserId,
                    rtsId = viewModel.RtsId,
                });
            }
        }

        return viewModel.Status switch
        {
            ModificationStatus.RequestForInformation =>
                await HandleRequestForInformation(viewModel, saveForLater),

            ModificationStatus.ResponseReviseAndAuthorise =>
                await HandleReviseAndAuthorise(viewModel, saveForLater),

            ModificationStatus.ResponseWithSponsor or
            ModificationStatus.ResponseRequestRevisions =>
                await HandleRequestRevisions(viewModel, saveForLater),

            _ => this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "Unsupported modification status for saving RFI Responses",
            })
        };
    }

    [NonAction]
    private async Task<IActionResult> HandleRequestForInformation(
    ModificationDetailsViewModel viewModel,
    bool saveForLater)
    {
        var responses = viewModel.RfiModel.RfiResponses
            .Select(r => r.InitialResponse.FirstOrDefault() ?? string.Empty)
            .ToList();

        var result = await projectModificationsService.SaveModificationRfiResponses(
            new ModificationRfiResponseRequest
            {
                ProjectModificationId = Guid.Parse(viewModel.ModificationId!),
                Responses = responses,
                Role = ResponseRoles.Applicant,
                ResponseOrigin = ResponseOrigin.InitialResponse
            });

        if (!result.IsSuccessStatusCode)
            return this.ServiceError(result);

        if (saveForLater)
        {
            return RedirectToAction(
             "ReviewAllChanges",
             "ReviewAllChanges",
             new
             {
                 projectRecordId = viewModel.ProjectRecordId,
                 irasId = viewModel.IrasId,
                 shortTitle = viewModel.ShortTitle,
                 projectModificationId = Guid.Parse(viewModel.ModificationId!)
             });
        }

        var redirectResult = await CheckDocumentsAndRedirectIfRequired(Guid.Parse(viewModel.ModificationId!));

        if (redirectResult is not null)
        {
            return redirectResult;
        }

        return RedirectToAction(nameof(RfiCheckAndSubmitResponses));
    }

    [NonAction]
    private async Task<IActionResult?> CheckDocumentsAndRedirectIfRequired(Guid modificationId)
    {
        // Fetch all modification documents
        var searchQuery = new ProjectOverviewDocumentSearchRequest
        {
            AllowedStatuses = User.GetAllowedStatuses(StatusEntitiy.Document)
        };

        var documentsResponse = await projectModificationsService.GetDocumentsForModification(
            modificationId,
            searchQuery,
            1,
            300,
            nameof(ProjectOverviewDocumentDto.DocumentType),
            SortDirections.Ascending);

        var modificationDocuments = documentsResponse?.Content?.Documents ?? [];

        // CHECK FOR INCOMPLETE DOCUMENTS
        if (modificationDocuments.Any())
        {
            var documentRequest = BuildDocumentRequest();
            var documentStatuses = await GetDocumentCompletionStatuses(documentRequest);

            var hasIncompleteDocuments = documentStatuses.Any(d =>
                d.Status.Equals(
                    DocumentDetailStatus.Incomplete.ToString(),
                    StringComparison.OrdinalIgnoreCase));

            if (hasIncompleteDocuments)
            {
                return RedirectToRoute("pmc:DocumentDetailsIncomplete");
            }
        }

        // CHECK FOR MALWARE SCAN STATUS
        var allMalwareScansSuccessful = modificationDocuments
            .All(d => d.IsMalwareScanSuccessful == true);

        if (!allMalwareScansSuccessful)
        {
            return RedirectToRoute("pmc:DocumentsScanInProgress");
        }

        return null;
    }

    [NonAction]
    private async Task<IActionResult> HandleReviseAndAuthorise(
    ModificationDetailsViewModel viewModel,
    bool saveForLater)
    {
        // Applicant – revised response
        var applicantResponses = viewModel.RfiModel.RfiResponses
            .Select(r => r.ReviseAndAuthorise.FirstOrDefault() ?? string.Empty)
            .ToList();

        var applicantResult = await projectModificationsService.SaveModificationRfiResponses(
            new ModificationRfiResponseRequest
            {
                ProjectModificationId = Guid.Parse(viewModel.ModificationId!),
                Responses = applicantResponses,
                Role = ResponseRoles.Applicant,
                ResponseOrigin = ResponseOrigin.ReviseAndAuthorise
            });

        if (!applicantResult.IsSuccessStatusCode)
            return this.ServiceError(applicantResult);

        // Sponsor – reasons for revise & authorise
        var sponsorResponses = viewModel.RfiModel.RfiResponses
            .Select(r => r.ReasonForReviseAndAuthorise.FirstOrDefault() ?? string.Empty)
            .ToList();

        var sponsorResult = await projectModificationsService.SaveModificationRfiResponses(
            new ModificationRfiResponseRequest
            {
                ProjectModificationId = Guid.Parse(viewModel.ModificationId!),
                Responses = sponsorResponses,
                Role = ResponseRoles.Sponsor,
                ResponseOrigin = ResponseOrigin.ReviseAndAuthorise
            });

        if (!sponsorResult.IsSuccessStatusCode)
            return this.ServiceError(sponsorResult);

        if (saveForLater)
        {
            return RedirectToRoute(
                "sws:modifications",
                new
                {
                    projectRecordId = viewModel.ProjectRecordId,
                    sponsorOrganisationUserId = viewModel.SponsorOrganisationUserId,
                    rtsId = viewModel.RtsId
                });
        }
        var redirectResult = await CheckDocumentsAndRedirectIfRequired(Guid.Parse(viewModel.ModificationId!));

        if (redirectResult is not null)
        {
            return redirectResult;
        }

        return RedirectToAction(nameof(RfiCheckAndSubmitResponses));
    }

    [HttpGet]
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Review)]
    public async Task<IActionResult> RfiCheckAndSubmitResponses()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsViewModel());
        viewModel.Status = (viewModel as BaseProjectModificationViewModel).Status;
        var rfiReasons = await projectModificationsService.GetModificationReviewResponses(viewModel.ProjectRecordId, Guid.Parse(viewModel.ModificationId));
        var rfiResponses = await projectModificationsService.GetModificationRfiResponses(viewModel.ProjectRecordId, Guid.Parse(viewModel.ModificationId));

        if (!rfiReasons.IsSuccessStatusCode)
        {
            this.ServiceError(rfiReasons);
        }
        if (!rfiResponses.IsSuccessStatusCode)
        {
            this.ServiceError(rfiResponses);
        }

        viewModel.RfiModel = new RfiDetailsViewModel
        {
            RfiReasons = rfiReasons?.Content?.RequestForInformationReasons ?? [],
            RfiResponses = rfiResponses?.Content?.RfiResponses ?? [],
        };
        return View(viewModel);
    }

    [HttpPost]
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Submit)]
    public async Task<IActionResult> RfiSubmitResponses()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsViewModel());
        viewModel.Status = (viewModel as BaseProjectModificationViewModel).Status;
        ServiceResponse updateStatusResponse;

        switch (viewModel.Status)
        {
            case ModificationStatus.RequestForInformation:
                updateStatusResponse = await projectModificationsService.UpdateModificationStatus(
                    new UpdateModificationStatusRequest
                    {
                        ProjectRecordId = viewModel.ProjectRecordId!,
                        ModificationId = Guid.Parse(viewModel.ModificationId!),
                        Status = ModificationStatus.ResponseWithSponsor
                    });
                break;

            case ModificationStatus.ResponseReviseAndAuthorise:
                updateStatusResponse = await projectModificationsService.UpdateModificationStatus(
                    new UpdateModificationStatusRequest
                    {
                        ProjectRecordId = viewModel.ProjectRecordId!,
                        ModificationId = Guid.Parse(viewModel.ModificationId!),
                        Status = ModificationStatus.ResponseWithReviewBody
                    });
                break;

            case ModificationStatus.ResponseWithSponsor:
                updateStatusResponse = await projectModificationsService.UpdateModificationStatus(
                    new UpdateModificationStatusRequest
                    {
                        ProjectRecordId = viewModel.ProjectRecordId!,
                        ModificationId = Guid.Parse(viewModel.ModificationId!),
                        Status = ModificationStatus.ResponseRequestRevisions
                    });
                break;

            default:
                updateStatusResponse = new ServiceResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Error = "Unsupported modification status for saving RFI Responses"
                };
                break;
        }

        if (!updateStatusResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateStatusResponse);
        }

        return RedirectToAction(nameof(RfiResponsesConfirmation));
    }

    [HttpGet]
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Review)]
    public IActionResult RfiResponsesConfirmation()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());
        return View(viewModel);
    }

    [NonAction]
    private async Task<IActionResult> HandleRequestRevisions(
    ModificationDetailsViewModel viewModel,
    bool saveForLater)
    {
        // sponsor request revision response
        var sponsorResponses = viewModel.RfiModel.RfiResponses
            .Select(r => r.RequestRevisionsBySponsor.FirstOrDefault() ?? string.Empty)
            .ToList();

        // applicant request revision response
        var applicantResponses = viewModel.RfiModel.RfiResponses
            .Select(r => r.RequestRevisionsByApplicant.FirstOrDefault() ?? string.Empty)
            .ToList();

        var applicantResult = await projectModificationsService.SaveModificationRfiResponses(
            new ModificationRfiResponseRequest
            {
                ProjectModificationId = Guid.Parse(viewModel.ModificationId!),
                Responses = viewModel.Status == ModificationStatus.ResponseWithSponsor ? sponsorResponses : applicantResponses,
                Role = viewModel.Status == ModificationStatus.ResponseWithSponsor ? ResponseRoles.Sponsor : ResponseRoles.Applicant,
                ResponseOrigin = ResponseOrigin.RequestRevisions
            });

        if (!applicantResult.IsSuccessStatusCode)
        {
            return this.ServiceError(applicantResult);
        }

        if (saveForLater)
        {
            if (viewModel.Status == ModificationStatus.ResponseWithSponsor)
            {
                return RedirectToRoute(
                    "sws:modifications",
                    new
                    {
                        projectRecordId = viewModel.ProjectRecordId,
                        sponsorOrganisationUserId = viewModel.SponsorOrganisationUserId,
                        rtsId = viewModel.RtsId
                    });
            }
            return RedirectToRoute(
              "pov:postapproval",
              new
              {
                  projectRecordId = viewModel.ProjectRecordId,
                  showBanner = true
              });
        }
        var redirectResult = await CheckDocumentsAndRedirectIfRequired(Guid.Parse(viewModel.ModificationId!));

        if (redirectResult is not null)
        {
            return redirectResult;
        }

        if (viewModel.Status == ModificationStatus.ResponseRequestRevisions)
        {
            var updateStatusResponse = await projectModificationsService.UpdateModificationStatus(
                    new UpdateModificationStatusRequest
                    {
                        ProjectRecordId = viewModel.ProjectRecordId!,
                        ModificationId = Guid.Parse(viewModel.ModificationId!),
                        Status = ModificationStatus.ResponseWithSponsor,
                    });
            if (!updateStatusResponse.IsSuccessStatusCode)
            {
                return this.ServiceError(updateStatusResponse);
            }
            return RedirectToAction(nameof(RfiResponsesConfirmation));
        }
        return RedirectToAction(nameof(RfiCheckAndSubmitResponses));
    }
}