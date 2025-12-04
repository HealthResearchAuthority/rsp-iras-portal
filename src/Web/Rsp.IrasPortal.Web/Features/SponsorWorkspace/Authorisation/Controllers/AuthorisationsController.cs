using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Controllers;

/// <summary>
///     Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class AuthorisationsController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<SponsorAuthorisationsSearchModel> searchValidator
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, null!)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private readonly IRespondentService _respondentService = respondentService;

    [Authorize(Policy = Permissions.Sponsor.Modifications_Search)]
    [HttpGet]
    public async Task<IActionResult> Authorisations
    (
        Guid sponsorOrganisationUserId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsModel.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    )
    {
        var model = new SponsorAuthorisationsViewModel();

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.SponsorAuthorisationsSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<SponsorAuthorisationsSearchModel>(json)!;
        }

        var searchQuery = new SponsorAuthorisationsSearchRequest
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
            RouteName = "sws:authorisations",
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
    [CmsContentAction(nameof(Authorisations))]
    public async Task<IActionResult> ApplyFilters(SponsorAuthorisationsViewModel model)
    {
        var validationResult = await searchValidator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Authorisations),
                new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
        }

        HttpContext.Session.SetString(SessionKeys.SponsorAuthorisationsSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Authorisations),
            new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
    }

    // 1) Shared builder used by both GET and POST
    private async Task<AuthoriseOutcomeViewModel?> BuildCheckAndAuthorisePageAsync
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
        config.ForType<ModificationDetailsViewModel, AuthoriseOutcomeViewModel>()
              .Ignore(dest => dest.ProjectOverviewDocumentViewModel);

        var authoriseOutcomeViewModel = modification.Adapt<AuthoriseOutcomeViewModel>(config);

        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        // Fetch the CMS question set that defines what metadata must be collected for this document.
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build the questionnaire model containing all questions for the details section.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);
        var matchingQuestion = questionnaire.Questions.FirstOrDefault(q => q.QuestionId == ModificationQuestionIds.DocumentType);

        searchQuery.DocumentTypes = matchingQuestion?.Answers?
            .ToDictionary(a => a.AnswerId, a => a.AnswerText) ?? [];

        var modificationDocumentsResponseResult = await projectModificationsService.GetDocumentsForModification(projectModificationId,
            searchQuery, pageNumber, pageSize, sortField, sortDirection);

        authoriseOutcomeViewModel.ProjectOverviewDocumentViewModel.Documents = modificationDocumentsResponseResult?.Content?.Documents ?? [];

        await MapDocumentTypesAndStatusesAsync(questionnaire, modification.ProjectOverviewDocumentViewModel.Documents);

        authoriseOutcomeViewModel.ProjectOverviewDocumentViewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationDocumentsResponseResult?.Content?.TotalCount ?? 0)
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

        return View(response);
    }

    // 3) POST: on invalid, rebuild the page VM and return the same view with ModelState errors
    [Authorize(Policy = Permissions.Sponsor.Modifications_Authorise)]
    [HttpPost]
    public async Task<IActionResult> CheckAndAuthorise(AuthoriseOutcomeViewModel model)
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

            default:
                await projectModificationsService.UpdateModificationStatus
                (
                    model.ProjectRecordId,
                    Guid.Parse(model.ModificationId),
                    ModificationStatus.NotAuthorised
                );

                break;
        }

        return RedirectToAction(nameof(Confirmation), model);
    }

    [Authorize(Policy = Permissions.Sponsor.Modifications_Review)]
    [HttpGet]
    public IActionResult Confirmation(AuthoriseOutcomeViewModel model)
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
}