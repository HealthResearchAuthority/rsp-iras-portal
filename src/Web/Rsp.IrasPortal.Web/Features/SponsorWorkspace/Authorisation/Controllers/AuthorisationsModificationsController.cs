using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Extensions;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class AuthorisationsModificationsController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ISponsorOrganisationService sponsorOrganisationService,
    ICmsQuestionsetService cmsQuestionsetService,
    ISponsorUserAuthorisationService sponsorUserAuthorisationService,
    IValidator<AuthorisationsModificationsSearchModel> searchValidator,
    IValidator<AuthoriseModificationsOutcomeViewModel> outcomeValidator,
    IValidator<QuestionnaireViewModel> questionnaireValidator,
    IFeatureManager featureManager,
    IRtsService rtsService,
    IApplicationsService applicationsService
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, questionnaireValidator, featureManager)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private const string SponsorReferenceCategoryId = "Sponsor reference";
    private readonly IRespondentService _respondentService = respondentService;

    [Authorize(Policy = Permissions.Sponsor.Modifications_Search)]
    [HttpGet]
    public async Task<IActionResult> Modifications
    (
        Guid sponsorOrganisationUserId,
        string rtsId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsModel.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    )
    {
        var auth = await sponsorUserAuthorisationService.AuthoriseWithOrganisationContextAsync(
            this, sponsorOrganisationUserId, User, rtsId);
        if (!auth.IsAuthorised)
        {
            return auth.FailureResult!;
        }

        var sponsorOrganisationUser =
            await sponsorOrganisationService.GetSponsorOrganisationUser(sponsorOrganisationUserId, rtsId);

        if (!sponsorOrganisationUser.IsSuccessStatusCode)
        {
            return this.ServiceError(sponsorOrganisationUser);
        }

        TempData[TempDataKeys.IsAuthoriser] = sponsorOrganisationUser.Content!.IsAuthoriser;

        var model = new AuthorisationsModificationsViewModel()
        {
            SponsorOrgansationCount = auth.SponsorOrganisationCount,
            SponsorOrganisationName = auth.SponsorOrganisationName,
            SponsorOrganisationUserId = sponsorOrganisationUserId,
            RtsId = rtsId
        };

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.SponsorAuthorisationsModificationsSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<AuthorisationsModificationsSearchModel>(json)!;
        }

        var searchQuery = new SponsorAuthorisationsModificationsSearchRequest
        {
            SearchTerm = model.Search.SearchTerm
        };

        // getting modifications by sponsor organisation name
        var projectModificationsServiceResponse =
            await projectModificationsService.GetModificationsBySponsorOrganisationUserId(sponsorOrganisationUserId,
                searchQuery, pageNumber, pageSize, sortField, sortDirection, rtsId);

        model.Modifications = projectModificationsServiceResponse?.Content?.Modifications?
            .Select(dto => new ModificationsModel
            {
                Id = dto.Id,
                ModificationId = dto.ModificationId,
                ShortProjectTitle = dto.ShortProjectTitle,
                ChiefInvestigatorFirstName = dto.ChiefInvestigatorFirstName,
                ChiefInvestigatorLastName = dto.ChiefInvestigatorLastName,
                ChiefInvestigator = dto.ChiefInvestigator,
                SponsorOrganisation = dto.SponsorOrganisation,
                ProjectRecordId = dto.ProjectRecordId,
                SentToRegulatorDate = dto.SentToRegulatorDate,
                SentToSponsorDate = dto.SentToSponsorDate,
                CreatedAt = dto.CreatedAt,
                Status = dto.Status,
            })
            .ToList() ?? [];

        model.Pagination = new PaginationViewModel(pageNumber, pageSize,
            projectModificationsServiceResponse?.Content?.TotalCount ?? 0)
        {
            RouteName = "sws:modifications",
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "authorisations-selection",
            AdditionalParameters = new Dictionary<string, string>
            {
                { "SponsorOrganisationUserId", sponsorOrganisationUserId.ToString() },
                { "RtsId", rtsId }
            }
        };

        model.SponsorOrganisationUserId = sponsorOrganisationUserId;
        model.RtsId = rtsId;

        return View(model);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Search)]
    [HttpPost]
    [CmsContentAction(nameof(Modifications))]
    public async Task<IActionResult> ApplyFilters(AuthorisationsModificationsViewModel model)
    {
        var validationResult = await searchValidator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(
                nameof(Modifications),
                new
                {
                    sponsorOrganisationUserId = model.SponsorOrganisationUserId,
                    rtsId = model.RtsId
                });
        }

        HttpContext.Session.SetString(SessionKeys.SponsorAuthorisationsModificationsSearch, JsonSerializer.Serialize(model.Search));

        return RedirectToAction(
            nameof(Modifications),
            new
            {
                sponsorOrganisationUserId = model.SponsorOrganisationUserId,
                rtsId = model.RtsId
            });
    }

    // 1) Shared builder used by both GET and POST
    private async Task<AuthoriseModificationsOutcomeViewModel?> BuildCheckAndAuthorisePageAsync
    (
        Guid projectModificationId,
        string irasId,
        string shortTitle,
        string projectRecordId,
        Guid sponsorOrganisationUserId,
        string rtsId,
        Guid? modificationChangeId = null,
    int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Ascending
    )
    {
        // Fetch the modification by its identifier
        var (result, modification) =
            await PrepareModificationAsync(projectModificationId, irasId, shortTitle, projectRecordId);
        if (result is not null)
        {
            return null;
        }
        // Store the modification details in TempData for later use

        // Load sponsor details Q&A
        var sponsorDetailsQuestionsResponse =
            await cmsQuestionsetService.GetModificationQuestionSet(SponsorDetailsSectionId);

        var sponsorDetailsResponse =
            await _respondentService.GetModificationAnswers(projectModificationId, projectRecordId);
        var sponsorDetailsAnswers = sponsorDetailsResponse.Content!;

        var sponsorDetailsQuestionnaire =
            QuestionsetHelpers.BuildQuestionnaireViewModel(sponsorDetailsQuestionsResponse.Content!);
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

        var config = new TypeAdapterConfig();
        config.ForType<ModificationDetailsViewModel, AuthoriseModificationsOutcomeViewModel>()
              .Ignore(dest => dest.ProjectOverviewDocumentViewModel);

        var authoriseOutcomeViewModel = modification.Adapt<AuthoriseModificationsOutcomeViewModel>(config);

        // get all documents for the modification and do pagination here
        var modificationDocumentsResponseResult = await this.GetModificationDocuments(projectModificationId,
            DocumentDetailsSection, 1, int.MaxValue, sortField, sortDirection);

        var allDocuments = modificationDocumentsResponseResult.Item1?.Content?.Documents ?? [];

        // Map the document types and statuses to user-friendly text for display in the view.
        await MapDocumentTypesAndStatusesAsync(modificationDocumentsResponseResult.Item2, allDocuments);

        // apply pagination
        var paginatedDocuments = GetSortedAndPaginatedDocuments(allDocuments, sortField, sortDirection, pageSize, pageNumber);

        authoriseOutcomeViewModel.ProjectOverviewDocumentViewModel.Documents = paginatedDocuments ?? [];

        authoriseOutcomeViewModel.ProjectOverviewDocumentViewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationDocumentsResponseResult.Item1?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "projectdocuments-selection",
            RouteName = "sws:CheckAndAuthorise",
            AdditionalParameters = new Dictionary<string, string>()
            {
                { "projectRecordId", projectRecordId },
                { "irasId", irasId },
                { "shortTitle", shortTitle },
                { "projectModificationId", projectModificationId.ToString() },
                { "sponsorOrganisationUserId", sponsorOrganisationUserId.ToString() },
                { "rtsId", rtsId },
            }
        };

        authoriseOutcomeViewModel.SponsorOrganisationUserId = sponsorOrganisationUserId.ToString();
        authoriseOutcomeViewModel.ProjectModificationId = projectModificationId;
        authoriseOutcomeViewModel.ModificationChangeId = modificationChangeId is null ? null : modificationChangeId.ToString();
        authoriseOutcomeViewModel.IrasId = irasId;
        authoriseOutcomeViewModel.ShortTitle = shortTitle;
        authoriseOutcomeViewModel.ProjectRecordId = projectRecordId;
        authoriseOutcomeViewModel.RtsId = rtsId;

        //For PDF download data population
        var reviewOutcomeModel = new ReviewOutcomeViewModel
        {
            ModificationDetails = modification,
        };

        TempData[TempDataKeys.ProjectModification.ProjectModificationsDetails] =
            JsonSerializer.Serialize(reviewOutcomeModel);

        return authoriseOutcomeViewModel;
    }

    // 2) GET stays tiny and calls the builder
    [Authorize(Policy = Permissions.Sponsor.Modifications_Review)]
    [HttpGet]
    public async Task<IActionResult> CheckAndAuthorise
    (
        string projectRecordId,
        string irasId,
        string shortTitle,
        Guid projectModificationId,
        Guid sponsorOrganisationUserId,
        string rtsId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Ascending
    )
    {
        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;

        var response =
            await BuildCheckAndAuthorisePageAsync
            (
                projectModificationId,
                irasId,
                shortTitle,
                projectRecordId,
                sponsorOrganisationUserId,
                rtsId,
                null,
                pageNumber,
                pageSize,
                sortField,
                sortDirection
            );

        var auth = await sponsorUserAuthorisationService.AuthoriseWithOrganisationContextAsync(this, sponsorOrganisationUserId, User, rtsId);
        if (!auth.IsAuthorised)
        {
            return auth.FailureResult!;
        }

        var sponsorOrganisationUser =
            await sponsorOrganisationService.GetSponsorOrganisationUser(sponsorOrganisationUserId, rtsId);

        if (!sponsorOrganisationUser.IsSuccessStatusCode)
        {
            return this.ServiceError(sponsorOrganisationUser);
        }

        TempData[TempDataKeys.IsAuthoriser] = sponsorOrganisationUser.Content!.IsAuthoriser;

        if (await featureManager.IsEnabledAsync(FeatureFlags.RevisionAndAuthorisation))
        {
            var reviewResponse = await projectModificationsService.GetModificationReviewResponses(projectRecordId, projectModificationId);

            if (!reviewResponse.IsSuccessStatusCode)
            {
                return this.ServiceError(reviewResponse);
            }

            ViewBag.RevisionSent = !string.IsNullOrWhiteSpace(reviewResponse.Content!.RevisionDescription);
        }

        return View(response);
    }

    // 3) POST: on invalid, rebuild the page VM and return the same view with ModelState errors
    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [HttpPost]
    public async Task<IActionResult> CheckAndAuthorise(AuthoriseModificationsOutcomeViewModel model)
    {
        // 🟢 Always build the page first, so it's hydrated for both success and error paths
        var hydrated = await BuildCheckAndAuthorisePageAsync(
            Guid.Parse(model.ModificationId),
            model.IrasId,
            model.ShortTitle,
            model.ProjectRecordId,
            Guid.Parse(model.SponsorOrganisationUserId),
            model.RtsId
        );

        if (!ModelState.IsValid)
        {
            var sponsorOrganisationUser = await sponsorOrganisationService.GetSponsorOrganisationUser(Guid.Parse(model.SponsorOrganisationUserId), model.RtsId);

            if (!sponsorOrganisationUser.IsSuccessStatusCode)
            {
                return this.ServiceError(sponsorOrganisationUser);
            }

            TempData[TempDataKeys.IsAuthoriser] = sponsorOrganisationUser.Content!.IsAuthoriser;

            if (await featureManager.IsEnabledAsync(FeatureFlags.RevisionAndAuthorisation))
            {
                var reviewResponse = await projectModificationsService.GetModificationReviewResponses(model.ProjectRecordId, model.ProjectModificationId);

                if (!reviewResponse.IsSuccessStatusCode)
                {
                    return this.ServiceError(reviewResponse);
                }

                ViewBag.RevisionSent = !string.IsNullOrWhiteSpace(reviewResponse.Content!.RevisionDescription);
            }

            // Preserve the posted Outcome so the radios keep the selection
            if (hydrated is not null)
            {
                hydrated.Outcome = model.Outcome;
                // copy any other posted fields you want to preserve on re-render
            }

            return View(hydrated);
        }

        var rfiFeatureFlagEnabled = await featureManager.IsEnabledAsync(FeatureFlags.RequestForInformation);

        switch (model.Outcome)
        {
            case "Authorised":
                // Default to "No review required" if ReviewType is null/empty
                var reviewType = string.IsNullOrWhiteSpace(model.ReviewType)
                    ? "No review required"
                    : model.ReviewType;
                //call modification service and check if any modificatios are in reviewbody status
                var modificationsResponse = await projectModificationsService.GetModificationsForProject(model.ProjectRecordId, new ModificationSearchRequest());
                if (modificationsResponse.Content?.Modifications?
                        .Any(m => m.Status is ModificationStatus.WithReviewBody or ModificationStatus.ResponseWithReviewBody) == true)
                {
                    return RedirectToAction(nameof(CanSubmitToReviewBody), model);
                }
                var specificAreaOfChangeId = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeText) as string;
                //Temporary project halt and restart of project
                (bool result, IActionResult error) = await UpdateProjectHaltAndRestartStatus(model.ProjectRecordId, specificAreaOfChangeId);
                if (!result)
                {
                    return error!;
                }

                switch (reviewType)
                {
                    case "Review required":
                        var isSponsorAuthorisationEnabled =
                            await featureManager.IsEnabledAsync(FeatureFlags.SponsorAuthorisation);

                        var status =
                            isSponsorAuthorisationEnabled && model.Status == ModificationStatus.ResponseWithSponsor
                                ? ModificationStatus.ResponseWithReviewBody
                                : ModificationStatus.WithReviewBody;

                        await projectModificationsService.UpdateModificationStatus(
                            new UpdateModificationStatusRequest
                            {
                                ProjectRecordId = model.ProjectRecordId,
                                ModificationId = Guid.Parse(model.ModificationId),
                                Status = status
                            });
                        break;

                    default:
                        await projectModificationsService.UpdateModificationStatus(
                            new UpdateModificationStatusRequest
                            {
                                ProjectRecordId = model.ProjectRecordId,
                                ModificationId = Guid.Parse(model.ModificationId),
                                Status = ModificationStatus.Approved
                            });
                        break;
                }

                break;

            case "RequestRevisions":
                return RedirectToAction(nameof(RequestRevisions), model);

            case "ReviseAndAuthorise":

                if (rfiFeatureFlagEnabled)
                {
                    await projectModificationsService.UpdateModificationStatus(
                        new UpdateModificationStatusRequest
                        {
                            ProjectRecordId = model.ProjectRecordId,
                            ModificationId = Guid.Parse(model.ModificationId),
                            Status = ModificationStatus.ReviseAndAuthorise
                        });
                }
                else
                {
                    await projectModificationsService.LegacyUpdateModificationStatus
                        (
                            model.ProjectRecordId,
                            Guid.Parse(model.ModificationId),
                            ModificationStatus.ReviseAndAuthorise,
                            string.Empty
                        );
                }

                return RedirectToRoute("pmc:ModificationDetails", new
                {
                    projectRecordId = model.ProjectRecordId,
                    irasId = model.IrasId,
                    shortTitle = model.ShortTitle,
                    projectModificationId = Guid.Parse(model.ModificationId),
                    sponsorOrganisationUserId = model.SponsorOrganisationUserId,
                    rtsId = model.RtsId,
                });

            case "NotAuthorised":
                if ((await featureManager.IsEnabledAsync(FeatureFlags.NotAuthorisedReason) && model.Status == ModificationStatus.WithSponsor) ||
                   (await featureManager.IsEnabledAsync(FeatureFlags.SponsorAuthorisation) && model.Status == ModificationStatus.ResponseWithSponsor))
                {
                    return RedirectToAction(nameof(ModificationNotAuthorised), model);
                }
                else
                {
                    if (rfiFeatureFlagEnabled)
                    {
                        await projectModificationsService.UpdateModificationStatus(
                            new UpdateModificationStatusRequest
                            {
                                ProjectRecordId = model.ProjectRecordId,
                                ModificationId = Guid.Parse(model.ModificationId),
                                Status = ModificationStatus.NotAuthorised,
                                ReasonNotApproved = model.ReasonNotApproved
                            });
                    }
                    else
                    {
                        await projectModificationsService.LegacyUpdateModificationStatus
                            (
                                model.ProjectRecordId,
                                Guid.Parse(model.ModificationId),
                                ModificationStatus.NotAuthorised,
                                model.RevisionDescription,
                                model.ReasonNotApproved
                            );
                    }
                }

                break;
        }

        return RedirectToAction(nameof(Confirmation), model);
    }

    private async Task<(bool flowControl, IActionResult value)> UpdateProjectHaltAndRestartStatus(string projectRecordId, string? specificAreaOfChangeId)
    {
        // update the project record status with halt status
        if (specificAreaOfChangeId == AreasOfChange.ProjectHalt || specificAreaOfChangeId == AreasOfChange.ProjectRestart)
        {
            // Resolve new project status
            var newStatus = specificAreaOfChangeId switch
            {
                AreasOfChange.ProjectHalt => ProjectRecordStatus.ProjectHalt,
                AreasOfChange.ProjectRestart => ProjectRecordStatus.Active,
            };

            var updateApplicationResponse = await applicationsService.UpdateProjectRecordStatus(projectRecordId, newStatus);

            if (!updateApplicationResponse.IsSuccessStatusCode)
            {
                return (false, this.ServiceError(updateApplicationResponse));
            }
            TempData[TempDataKeys.ProjectRecordStatus] = newStatus;
        }
        return (true, null);
    }

    /// <summary>
    /// Warning message controller
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = Permissions.Sponsor.Modifications_Review)]
    [HttpGet]
    public IActionResult CanSubmitToReviewBody(AuthoriseModificationsOutcomeViewModel model)
    {
        return View("CanSubmitToReviewBody", model);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(FeatureFlags.RevisionAndAuthorisation)]
    [HttpGet]
    public async Task<IActionResult> RequestRevisions(AuthoriseModificationsOutcomeViewModel model)
    {
        var auth = await sponsorUserAuthorisationService.AuthoriseWithOrganisationContextAsync(this, Guid.Parse(model.SponsorOrganisationUserId), User, model.RtsId);
        if (!auth.IsAuthorised)
        {
            return auth.FailureResult!;
        }

        var sponsorOrganisationUser =
            await sponsorOrganisationService.GetSponsorOrganisationUser(Guid.Parse(model.SponsorOrganisationUserId), model.RtsId);

        if (!sponsorOrganisationUser.IsSuccessStatusCode)
        {
            return this.ServiceError(sponsorOrganisationUser);
        }

        var reviewResponse = await projectModificationsService.GetModificationReviewResponses(model.ProjectRecordId, model.ProjectModificationId);

        if (!reviewResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(reviewResponse);
        }

        var revisionDescription = reviewResponse.Content!.RevisionDescription;

        if (sponsorOrganisationUser.Content!.IsAuthoriser && string.IsNullOrWhiteSpace(revisionDescription))
        {
            return View(model);
        }
        else
        {
            return Forbid();
        }
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(FeatureFlags.RevisionAndAuthorisation)]
    [HttpPost]
    [CmsContentAction(nameof(RequestRevisions))]
    public async Task<IActionResult> SendRequestRevisions(AuthoriseModificationsOutcomeViewModel model)
    {
        var context = new ValidationContext<AuthoriseModificationsOutcomeViewModel>(model);
        var validationResult = await outcomeValidator.ValidateAsync(context);

        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (!ModelState.IsValid)
        {
            var sponsorOrganisationUser = await sponsorOrganisationService.GetSponsorOrganisationUser(Guid.Parse(model.SponsorOrganisationUserId), model.RtsId);

            if (!sponsorOrganisationUser.IsSuccessStatusCode)
            {
                return this.ServiceError(sponsorOrganisationUser);
            }

            var reviewResponse = await projectModificationsService.GetModificationReviewResponses(model.ProjectRecordId, model.ProjectModificationId);

            if (!reviewResponse.IsSuccessStatusCode)
            {
                return this.ServiceError(reviewResponse);
            }

            var revisionDescription = reviewResponse.Content!.RevisionDescription;

            if (sponsorOrganisationUser.Content!.IsAuthoriser && string.IsNullOrWhiteSpace(revisionDescription))
            {
                return View(nameof(RequestRevisions), model);
            }
            else
            {
                return Forbid();
            }
        }

        var rfiFeatureFlagEnabled = await featureManager.IsEnabledAsync(FeatureFlags.RequestForInformation);

        if (rfiFeatureFlagEnabled)
        {
            await projectModificationsService.UpdateModificationStatus(
                new UpdateModificationStatusRequest
                {
                    ProjectRecordId = model.ProjectRecordId,
                    ModificationId = Guid.Parse(model.ModificationId),
                    Status = ModificationStatus.RequestRevisions,
                    ReasonNotApproved = null,
                    Response = model.RevisionDescription,
                    Role = ResponseRoles.Sponsor,
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
                    model.RevisionDescription
                );
        }

        return RedirectToAction(nameof(Confirmation), model);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(FeatureFlags.RevisionAndAuthorisation)]
    [HttpPost]
    public async Task<IActionResult> AuthoriseRevision(AuthoriseModificationsOutcomeViewModel model, bool isSaveForLater)
    {
        bool skipValidation = isSaveForLater && string.IsNullOrWhiteSpace(model.RevisionDescription);
        model.Outcome = "ReviseAndAuthorise";
        ModelState.Remove(nameof(model.Outcome));
        if (!skipValidation)
        {
            var context = new ValidationContext<AuthoriseModificationsOutcomeViewModel>(model);
            var validationResult = await outcomeValidator.ValidateAsync(context);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                TempData.Remove(TempDataKeys.ModelState);
                TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
                TempData[TempDataKeys.RevisionDescription] = model.RevisionDescription;

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

        var rfiFeatureFlagEnabled = await featureManager.IsEnabledAsync(FeatureFlags.RequestForInformation);

        if (isSaveForLater)
        {
            if (rfiFeatureFlagEnabled)
            {
                await projectModificationsService.UpdateModificationStatus(
                    new UpdateModificationStatusRequest
                    {
                        ProjectRecordId = model.ProjectRecordId,
                        ModificationId = Guid.Parse(model.ModificationId),
                        Status = ModificationStatus.ReviseAndAuthorise,
                        ReasonNotApproved = null,
                        Response = model.RevisionDescription,
                        Role = ResponseRoles.Sponsor,
                        ResponseOrigin = ResponseOrigin.ReviseAndAuthorise
                    });
            }
            else
            {
                await projectModificationsService.LegacyUpdateModificationStatus
                    (
                        model.ProjectRecordId,
                        Guid.Parse(model.ModificationId),
                        ModificationStatus.ReviseAndAuthorise,
                        model.RevisionDescription
                    );
            }

            TempData[TempDataKeys.ShowNotificationBanner] = true;
            return RedirectToAction(nameof(Modifications), new { sponsorOrganisationUserId = model.SponsorOrganisationUserId, rtsId = model.RtsId });
        }
        (bool isValid, IActionResult result) = IsModificationChangeExist(model);
        if (!isValid)
        {
            return result;
        }
        // Fetch all modification documents (up to 200)
        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        searchQuery.AllowedStatuses = User.GetAllowedStatuses(StatusEntitiy.Document);
        var modificationDocumentsResponseResult = await projectModificationsService.GetDocumentsForModification(
            Guid.Parse(model.ModificationId),
            searchQuery,
            1,
            300,
            nameof(ProjectOverviewDocumentDto.DocumentType),
            SortDirections.Ascending);

        var modificationDocuments = modificationDocumentsResponseResult?.Content?.Documents ?? [];

        // CHECK FOR INCOMPLETE DOCUMENTS
        if (modificationDocuments.Any())
        {
            var documentRequest = BuildDocumentRequest();
            var documentStatuses = await GetDocumentCompletionStatuses(documentRequest);

            bool areDocumentsIncomplete = documentStatuses
                .Any(d => d.Status.Equals(DocumentDetailStatus.Incomplete.ToString(), StringComparison.OrdinalIgnoreCase));

            if (areDocumentsIncomplete)
            {
                return RedirectToRoute("pmc:DocumentDetailsIncomplete");
            }
        }

        // CHECK FOR MALWARE SCAN STATUS
        bool allMalwareScansSuccessful = modificationDocuments.All(d => d.IsMalwareScanSuccessful == true);

        if (!allMalwareScansSuccessful)
        {
            return RedirectToRoute("pmc:DocumentsScanInProgress");
        }

        // CHECK FOR COMPLETE SPONSOR REFERENCE
        var viewModel = await this.BuildSponsorQuestionnaireViewModel(Guid.Parse(model.ModificationId), model.ProjectRecordId, SponsorReferenceCategoryId);
        var isValidSponsorReferece = await this.ValidateQuestionnaire(questionnaireValidator, viewModel, true);

        if (!isValidSponsorReferece)
        {
            return View("SponsorReference", viewModel);
        }

        var reviewType = string.IsNullOrWhiteSpace(model.ReviewType)
                    ? "No review required"
                    : model.ReviewType;
        //call modification service and check if any modifications are in reviewbody status
        var modificationsResponse = await projectModificationsService.GetModificationsForProject(model.ProjectRecordId, new ModificationSearchRequest());

        if (rfiFeatureFlagEnabled)
        {
            if (modificationsResponse.Content?.Modifications?
                .Any(m => m.Status == ModificationStatus.WithReviewBody) == true)
            {
                await projectModificationsService.UpdateModificationStatus(
                    new UpdateModificationStatusRequest
                    {
                        ProjectRecordId = model.ProjectRecordId,
                        ModificationId = Guid.Parse(model.ModificationId),
                        Status = ModificationStatus.ReviseAndAuthorise,
                        ReasonNotApproved = null,
                        Response = model.RevisionDescription,
                        Role = ResponseRoles.Sponsor,
                        ResponseOrigin = ResponseOrigin.ReviseAndAuthorise
                    });
                return RedirectToAction(nameof(CanSubmitToReviewBody), model);
            }
            switch (reviewType)
            {
                case "Review required":
                    await projectModificationsService.UpdateModificationStatus(
                        new UpdateModificationStatusRequest
                        {
                            ProjectRecordId = model.ProjectRecordId,
                            ModificationId = Guid.Parse(model.ModificationId),
                            Status = ModificationStatus.WithReviewBody,
                            ReasonNotApproved = null,
                            Response = model.RevisionDescription,
                            Role = ResponseRoles.Sponsor,
                            ResponseOrigin = ResponseOrigin.ReviseAndAuthorise
                        });
                    break;

                default:
                    await projectModificationsService.UpdateModificationStatus(
                        new UpdateModificationStatusRequest
                        {
                            ProjectRecordId = model.ProjectRecordId,
                            ModificationId = Guid.Parse(model.ModificationId),
                            Status = ModificationStatus.Approved,
                            ReasonNotApproved = null,
                            Response = model.RevisionDescription,
                            Role = ResponseRoles.Sponsor,
                            ResponseOrigin = ResponseOrigin.ReviseAndAuthorise
                        });
                    break;
            }
        }
        else
        {
            if (modificationsResponse.Content?.Modifications?
                .Any(m => m.Status == ModificationStatus.WithReviewBody) == true)
            {
                await projectModificationsService.LegacyUpdateModificationStatus
                    (
                        model.ProjectRecordId,
                        Guid.Parse(model.ModificationId),
                        ModificationStatus.ReviseAndAuthorise,
                        model.RevisionDescription
                    );
                return RedirectToAction(nameof(CanSubmitToReviewBody), model);
            }
            switch (reviewType)
            {
                case "Review required":
                    await projectModificationsService.LegacyUpdateModificationStatus
                    (
                        model.ProjectRecordId,
                        Guid.Parse(model.ModificationId),
                        ModificationStatus.WithReviewBody,
                        model.RevisionDescription
                    );
                    break;

                default:
                    await projectModificationsService.LegacyUpdateModificationStatus
                    (
                        model.ProjectRecordId,
                        Guid.Parse(model.ModificationId),
                        ModificationStatus.Approved,
                        model.RevisionDescription
                    );
                    break;
            }
        }

        //Temporary project halt and restart of project
        (bool flowControl, IActionResult value) = await UpdateProjectHaltAndRestartStatus(model.ProjectRecordId!, model.ModificationChanges?.FirstOrDefault()?.SpecificAreaOfChangeId);
        if (!flowControl)
        {
            return value;
        }
        model.Outcome = "Authorised";
        return RedirectToAction(nameof(Confirmation), model);
    }

    private (bool flowControl, IActionResult value) IsModificationChangeExist(AuthoriseModificationsOutcomeViewModel model)
    {
        if (model.ModificationChanges == null || model.ModificationChanges.Count == 0)
        {
            return (flowControl: false, value: View("NoChangesToSubmit", new BaseProjectModificationViewModel
            {
                ProjectRecordId = model.ProjectRecordId,
                IrasId = model.IrasId,
                ShortTitle = model.ShortTitle,
                ModificationId = model.ModificationId,
                ModificationIdentifier = model.ModificationIdentifier,
                RtsId = model.RtsId,
                SponsorOrganisationUserId = model.SponsorOrganisationUserId.ToString(),
                Status = model.Status,
                DateCreated = model.DateCreated
            }));
        }

        return (flowControl: true, value: null);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Review)]
    [HttpGet]
    public IActionResult Confirmation(AuthoriseModificationsOutcomeViewModel model)
    {
        return View(model);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Review)]
    [HttpGet]
    public async Task<IActionResult> ChangeDetails
    (
        string projectRecordId,
        string irasId,
        string shortTitle,
        Guid projectModificationId,
        Guid sponsorOrganisationUserId,
        Guid modificationChangeId,
        string rtsId

    )
    {
        var response =
            await BuildCheckAndAuthorisePageAsync(projectModificationId, irasId, shortTitle, projectRecordId,
                sponsorOrganisationUserId, rtsId, modificationChangeId);

        return View(response);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(RequirementType.Any, FeatureFlags.NotAuthorisedReason, FeatureFlags.SponsorAuthorisation)]
    [HttpGet]
    public async Task<IActionResult> ModificationNotAuthorised(AuthoriseModificationsOutcomeViewModel model)
    {
        return await ValidateRequest(model);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(RequirementType.Any, FeatureFlags.NotAuthorisedReason, FeatureFlags.SponsorAuthorisation)]
    [CmsContentAction(nameof(ModificationNotAuthorised))]
    [HttpPost]
    public async Task<IActionResult> SaveModificationReasonNotApproved(AuthoriseModificationsOutcomeViewModel model)
    {
        var context = new ValidationContext<AuthoriseModificationsOutcomeViewModel>(model);
        var validationResult = await outcomeValidator.ValidateAsync(context);

        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (!ModelState.IsValid)
        {
            return await ValidateRequest(model);
        }

        var rfiFeatureFlagEnabled = await featureManager.IsEnabledAsync(FeatureFlags.RequestForInformation);

        if (rfiFeatureFlagEnabled)
        {
            await projectModificationsService.UpdateModificationStatus(
                new UpdateModificationStatusRequest
                {
                    ProjectRecordId = model.ProjectRecordId,
                    ModificationId = Guid.Parse(model.ModificationId),
                    Status = ModificationStatus.NotAuthorised,
                    ReasonNotApproved = model.ReasonNotApproved
                });
        }
        else
        {
            await projectModificationsService.LegacyUpdateModificationStatus
                (
                    model.ProjectRecordId,
                    Guid.Parse(model.ModificationId),
                    ModificationStatus.NotAuthorised,
                    model.RevisionDescription,
                    model.ReasonNotApproved
                );
        }

        return RedirectToAction(nameof(ConfirmationModificationNotAuthorised), model);
    }

    /// <summary>
    /// Confirmation view for modification not authorised
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(RequirementType.Any, FeatureFlags.NotAuthorisedReason, FeatureFlags.SponsorAuthorisation)]
    [CmsContentAction(nameof(ConfirmationModificationNotAuthorised))]
    [HttpGet]
    public IActionResult ConfirmationModificationNotAuthorised(AuthoriseModificationsOutcomeViewModel model)
    {
        return View("ConfirmModificationNotAuthorised", model);
    }

    /// <summary>
    /// Validate browser back request
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private async Task<IActionResult> ValidateRequest(AuthoriseModificationsOutcomeViewModel model)
    {
        var sponsorOrganisationUser = await sponsorOrganisationService.GetSponsorOrganisationUser(Guid.Parse(model.SponsorOrganisationUserId), model.RtsId);

        if (!sponsorOrganisationUser.IsSuccessStatusCode)
        {
            return this.ServiceError(sponsorOrganisationUser);
        }

        var response = await projectModificationsService.GetModificationReviewResponses(model.ProjectRecordId, model.ProjectModificationId);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        var reasonNotApproved = response.Content!.ReasonNotApproved;

        if (sponsorOrganisationUser.Content!.IsAuthoriser && string.IsNullOrWhiteSpace(reasonNotApproved))
        {
            return View("ModificationNotAuthorised", model);
        }
        else
        {
            return Forbid();
        }
    }
}