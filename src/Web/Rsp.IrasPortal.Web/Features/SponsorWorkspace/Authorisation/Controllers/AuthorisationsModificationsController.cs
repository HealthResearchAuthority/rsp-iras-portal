using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Services;
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
    IFeatureManager featureManager
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, null!)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private readonly IRespondentService _respondentService = respondentService;

    [Authorize(Policy = Permissions.Sponsor.Modifications_Search)]
    [HttpGet]
    public async Task<IActionResult> Modifications
    (
        Guid sponsorOrganisationUserId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsModel.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    )
    {
        var auth = await sponsorUserAuthorisationService.AuthoriseAsync(this, sponsorOrganisationUserId, User);
        if (!auth.IsAuthorised) return auth.FailureResult!;

        var model = new AuthorisationsModificationsViewModel();

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
                searchQuery, pageNumber, pageSize, sortField, sortDirection);

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
                { "SponsorOrganisationUserId", sponsorOrganisationUserId.ToString() }
            }
        };

        model.SponsorOrganisationUserId = sponsorOrganisationUserId;

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
            return RedirectToAction(nameof(Modifications),
                new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
        }

        HttpContext.Session.SetString(SessionKeys.SponsorAuthorisationsModificationsSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Modifications),
            new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
    }

    // 1) Shared builder used by both GET and POST
    private async Task<AuthoriseModificationsOutcomeViewModel?> BuildCheckAndAuthorisePageAsync
    (
        Guid projectModificationId,
        string irasId,
        string shortTitle,
        string projectRecordId,
        Guid sponsorOrganisationUserId,
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

        var modificationDocumentsResponseResult = await this.GetModificationDocuments(projectModificationId,
            DocumentDetailsSection, pageNumber, pageSize, sortField, sortDirection);

        authoriseOutcomeViewModel.ProjectOverviewDocumentViewModel.Documents = modificationDocumentsResponseResult.Item1?.Content?.Documents ?? [];

        await MapDocumentTypesAndStatusesAsync(modificationDocumentsResponseResult.Item2, modification.ProjectOverviewDocumentViewModel.Documents);

        authoriseOutcomeViewModel.ProjectOverviewDocumentViewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationDocumentsResponseResult.Item1?.Content?.TotalCount ?? 0)
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

        authoriseOutcomeViewModel.SponsorOrganisationUserId = sponsorOrganisationUserId;
        authoriseOutcomeViewModel.ProjectModificationId = projectModificationId;
        authoriseOutcomeViewModel.ModificationChangeId = modificationChangeId is null ? null : modificationChangeId.ToString();
        authoriseOutcomeViewModel.IrasId = irasId;
        authoriseOutcomeViewModel.ShortTitle = shortTitle;
        authoriseOutcomeViewModel.ProjectRecordId = projectRecordId;

        return authoriseOutcomeViewModel;
    }

    // 2) GET stays tiny and calls the builder
    [Authorize(Policy = Permissions.Sponsor.Modifications_Review)]
    [HttpGet]
    public async Task<IActionResult> CheckAndAuthorise(string projectRecordId, string irasId, string shortTitle,
        Guid projectModificationId, Guid sponsorOrganisationUserId)
    {
        var response =
            await BuildCheckAndAuthorisePageAsync(projectModificationId, irasId, shortTitle, projectRecordId,
                sponsorOrganisationUserId);

        var sponsorOrganisationUser =
            await sponsorOrganisationService.GetSponsorOrganisationUser(sponsorOrganisationUserId);

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
            model.SponsorOrganisationUserId
        );

        if (!ModelState.IsValid)
        {
            var sponsorOrganisationUser = await sponsorOrganisationService.GetSponsorOrganisationUser(model.SponsorOrganisationUserId);

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
                        .Any(m => m.Status == ModificationStatus.WithReviewBody) == true)
                {
                    return RedirectToAction(nameof(CanSubmitToReviewBody), model);
                }
                switch (reviewType)
                {
                    case "Review required":
                        await projectModificationsService.UpdateModificationStatus
                        (
                            model.ProjectRecordId,
                            Guid.Parse(model.ModificationId),
                            ModificationStatus.WithReviewBody
                        );
                        break;

                    default:
                        await projectModificationsService.UpdateModificationStatus
                        (
                            model.ProjectRecordId,
                            Guid.Parse(model.ModificationId),
                            ModificationStatus.Approved
                        );
                        break;
                }

                break;

            case "RequestRevisions":
                return RedirectToAction(nameof(RequestRevisions), model);

            case "NotAuthorised":
                if (await featureManager.IsEnabledAsync(FeatureFlags.NotAuthorisedReason))
                {
                    return RedirectToAction(nameof(ModificationNotAuthorised), model);
                }
                else
                {
                    await projectModificationsService.UpdateModificationStatus
                  (
                      model.ProjectRecordId,
                      Guid.Parse(model.ModificationId),
                      ModificationStatus.NotAuthorised,
                      model.RevisionDescription,
                      model.ReasonNotApproved
                  );
                }

                break;
        }

        return RedirectToAction(nameof(Confirmation), model);
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
        var sponsorOrganisationUser = await sponsorOrganisationService.GetSponsorOrganisationUser(model.SponsorOrganisationUserId);

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
            var sponsorOrganisationUser = await sponsorOrganisationService.GetSponsorOrganisationUser(model.SponsorOrganisationUserId);

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

        await projectModificationsService.UpdateModificationStatus
                (
                    model.ProjectRecordId,
                    Guid.Parse(model.ModificationId),
                    ModificationStatus.RequestRevisions,
                    model.RevisionDescription
                );

        return RedirectToAction(nameof(Confirmation), model);
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
        Guid modificationChangeId
    )
    {
        var response =
            await BuildCheckAndAuthorisePageAsync(projectModificationId, irasId, shortTitle, projectRecordId,
                sponsorOrganisationUserId, modificationChangeId);

        return View(response);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(FeatureFlags.NotAuthorisedReason)]
    [HttpGet]
    public async Task<IActionResult> ModificationNotAuthorised(AuthoriseModificationsOutcomeViewModel model)
    {
        return await ValidateRequest(model);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(FeatureFlags.NotAuthorisedReason)]
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

        await projectModificationsService.UpdateModificationStatus
               (
                   model.ProjectRecordId,
                   Guid.Parse(model.ModificationId),
                   ModificationStatus.NotAuthorised,
                   model.RevisionDescription,
                   model.ReasonNotApproved
               );
        return RedirectToAction(nameof(ConfirmationModificationNotAuthorised), model);
    }

    /// <summary>
    /// Confirmation view for modification not authorised
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [FeatureGate(FeatureFlags.NotAuthorisedReason)]
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
        var sponsorOrganisationUser = await sponsorOrganisationService.GetSponsorOrganisationUser(model.SponsorOrganisationUserId);

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