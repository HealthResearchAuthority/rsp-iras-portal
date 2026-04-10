using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// Manages searching, selecting and removing participating organisations for a modification change.
/// </summary>
[Authorize(Policy = Workspaces.MyResearch)]
[Route("modifications/participatingorganisations/[action]", Name = "porgs:[action]")]
public class ParticipatingOrganisationsController
(
    IRtsService rtsService,
    IRespondentService respondentService,
    IValidator<SearchOrganisationViewModel> searchOrganisationValidator
) : Controller
{
    /// <summary>
    /// Displays the initial participating organisation search page.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_ParticipatingOrganisations_Search)]
    [HttpGet("/modifications/participatingorganisations", Name = "pmc:[action]")]
    public async Task<IActionResult> ParticipatingOrganisations()
    {
        // Restore base modification context (project/modification identifiers) from TempData.
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new SearchOrganisationViewModel());

        return View(nameof(SearchOrganisation), viewModel);
    }

    /// <summary>
    /// Searches organisations using the supplied filters and paging options.
    /// Restores and persists filter state through TempData so paging/sorting keeps the same criteria.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_ParticipatingOrganisations_Search)]
    [CmsContentAction(nameof(ParticipatingOrganisations))]
    public async Task<IActionResult> SearchOrganisation
    (
        int pageNumber = 1,
        int pageSize = 10,
        [FromForm] SearchOrganisationViewModel? model = null,
        string? sortField = nameof(OrganisationModel.Name),
        string? sortDirection = SortDirections.Ascending
    )
    {
        // Initialize posted model and paging defaults.
        model ??= new SearchOrganisationViewModel();
        model.Search ??= new OrganisationSearchModel();
        model.Pagination ??= new PaginationViewModel(pageNumber, pageSize, 0)
        {
            SortDirection = sortDirection!.ToLower(),
            SortField = sortField!.ToLower(),
            FormName = "organisation-selection"
        };

        // For GET requests (paging/filter-chip actions), restore the last search criteria.
        if (Request.Method == "GET" && TempData[TempDataKeys.OrganisationSearchModel] is string orgSearchJson)
        {
            var orgSearchModel = JsonSerializer.Deserialize<OrganisationSearchModel>(orgSearchJson);

            if (orgSearchModel != null)
            {
                model.Search = orgSearchModel;
            }
        }

        var viewModel = TempData.PopulateBaseProjectModificationProperties(model);

        var validationResult = await searchOrganisationValidator.ValidateAsync(new ValidationContext<SearchOrganisationViewModel>(viewModel));

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(SearchOrganisation), model);
        }

        // Build requests to fetch already-selected organisations so they can be excluded from search results.
        var existingOrgsTasks = new List<Task<ServiceResponse<IEnumerable<ParticipatingOrganisationDto>>>>();

        // These specific areas share a pool, so both sets are excluded to prevent duplicate/confusing selections.
        switch (viewModel.SpecificAreaOfChangeId)
        {
            case SpecificAreasOfChange.AddNewSites:
            case SpecificAreasOfChange.AddNewPics:
                existingOrgsTasks.Add(respondentService.GetModificationParticipatingOrganisationsBySpecificArea(viewModel.ProjectRecordId, SpecificAreasOfChange.AddNewSites));
                existingOrgsTasks.Add(respondentService.GetModificationParticipatingOrganisationsBySpecificArea(viewModel.ProjectRecordId, SpecificAreasOfChange.AddNewPics));
                break;

            case SpecificAreasOfChange.EarlyClosureSites:
            case SpecificAreasOfChange.EarlyClosuresPics:
                existingOrgsTasks.Add(respondentService.GetModificationParticipatingOrganisationsBySpecificArea(viewModel.ProjectRecordId, SpecificAreasOfChange.EarlyClosureSites));
                existingOrgsTasks.Add(respondentService.GetModificationParticipatingOrganisationsBySpecificArea(viewModel.ProjectRecordId, SpecificAreasOfChange.EarlyClosuresPics));
                break;
        }

        var existingSelectedOrgsResponse = await Task.WhenAll(existingOrgsTasks);

        if (existingSelectedOrgsResponse.Any(r => !r.IsSuccessStatusCode))
        {
            return this.ServiceError(existingSelectedOrgsResponse.First(r => !r.IsSuccessStatusCode));
        }

        var selectedOrganisationsIds = existingSelectedOrgsResponse
                .SelectMany(r => r.Content ?? [])
                .Select(o => o.OrganisationId)
                .ToList();

        var organisationsSearchRequest = new OrganisationsSearchRequest
        {
            SearchNameTerm = model.Search.SearchNameTerm,
            Countries = model.Search.Countries,
            OrganisationTypes = model.Search.OrganisationTypes,
            // UI labels are converted to backend boolean status values.
            OrganisationStatuses = model.Search.OrganisationStatuses.ConvertAll(status => status switch
            {
                "Active organisations" => true,
                "Terminated organisations" => false,
                _ => false
            })
        };

        // Request extra rows to compensate for organisations that will be filtered out after retrieval.
        var response = await rtsService.SearchOrganisations(organisationsSearchRequest, pageNumber, pageSize + selectedOrganisationsIds.Count, sortDirection!, sortField!);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        // Remove organisations already selected in this modification context.
        response.Content!.Organisations = [.. response.Content!.Organisations.ExceptBy(selectedOrganisationsIds, o => o.Id).Take(pageSize)];

        viewModel.Organisations = response.Content?.Organisations?.Select(dto => new SelectableOrganisationViewModel
        {
            Organisation = new OrganisationModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Address = dto.Address,
                CountryName = dto.CountryName,
                Type = dto.Type
            }
        }).ToList() ?? [];

        // Adjust total count for pagination to reflect the filtered results.
        var totalCount = response.Content?.TotalCount - selectedOrganisationsIds.Count ?? 0;

        viewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, totalCount)
        {
            SortDirection = sortDirection!.ToLower(),
            SortField = sortField!.ToLower(),
            FormName = "organisation-selection"
        };

        // Persist filter state and last result shape for validation round-trips.
        TempData[TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(model.Search);
        TempData[TempDataKeys.OrganisationSearchResults] = JsonSerializer.Serialize(viewModel);

        return View(viewModel);
    }

    /// <summary>
    /// Persists selected organisations and routes either to the next step or save-for-later destination.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpPost]
    public async Task<IActionResult> ConfirmSelection(SearchOrganisationViewModel model, bool saveForLater, string sortField = "name", string sortDirection = "asc")
    {
        var searchOrganisationViewModel = TempData.PopulateBaseProjectModificationProperties(model);

        // At least one organisation must be selected unless user explicitly saves for later.
        if (!saveForLater && searchOrganisationViewModel.Organisations?.Any(org => org.IsSelected) is not true)
        {
            ModelState.AddModelError("participating-organisations", "Please select at least one organisation.");

            // Rehydrate the previous result set so the user returns to the same context.
            if (TempData[TempDataKeys.OrganisationSearchResults] is string orgSearchResultsJson)
            {
                var orgSearchResults = JsonSerializer.Deserialize<SearchOrganisationViewModel>(orgSearchResultsJson);

                if (orgSearchResults != null)
                {
                    searchOrganisationViewModel.Organisations = orgSearchResults.Organisations;
                    searchOrganisationViewModel.Pagination = orgSearchResults.Pagination;
                }

                return View(nameof(SearchOrganisation), searchOrganisationViewModel);
            }

            searchOrganisationViewModel.Pagination = new PaginationViewModel(1, 10, searchOrganisationViewModel.Organisations?.Count ?? 0)
            {
                SortField = sortField,
                SortDirection = sortDirection
            };

            return View(nameof(SearchOrganisation), searchOrganisationViewModel);
        }

        var selectedOrganisations = searchOrganisationViewModel?.Organisations?
            .Where(o => o.IsSelected)
            .Select(o => o.Organisation)
            .ToList() ?? [];

        if (selectedOrganisations.Count > 0)
        {
            var modificationParticipatingOrganisations = selectedOrganisations.ConvertAll(org => new ParticipatingOrganisationDto
            {
                ProjectModificationChangeId = Guid.Parse(searchOrganisationViewModel!.ModificationChangeId ?? Guid.Empty.ToString()),
                OrganisationId = org.Id,
                ProjectRecordId = searchOrganisationViewModel.ProjectRecordId,
                UserId = (HttpContext.Items[ContextItemKeys.UserId] as string)!,
            });

            var response = await respondentService.SaveModificationParticipatingOrganisations(modificationParticipatingOrganisations);

            if (!response.IsSuccessStatusCode)
            {
                return this.ServiceError(response);
            }
        }

        if (saveForLater)
        {
            var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;
            var rtsId = TempData.Peek(TempDataKeys.RevisionRtsId) as string ?? string.Empty;
            var status = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationStatus) as string ?? string.Empty;
            var sponsorOrganisationUserId = TempData.Peek(TempDataKeys.RevisionSponsorOrganisationUserId);

            // Used by the destination page to show save confirmation state.
            TempData[TempDataKeys.ShowNotificationBanner] = true;
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();

            if (status is ModificationStatus.ReviseAndAuthorise)
            {
                return RedirectToRoute("sws:modifications", new { sponsorOrganisationUserId, rtsId });
            }

            return RedirectToRoute("pov:postapproval", new { projectRecordId });
        }

        return RedirectToRoute("porgs:selectedparticipatingorganisations");
    }

    /// <summary>
    /// Displays currently selected participating organisations for the active modification change.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpGet]
    public async Task<IActionResult> SelectedParticipatingOrganisations()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new SelectedOrganisationsViewModel());

        var response = await respondentService.GetModificationParticipatingOrganisations(Guid.Parse(viewModel.ModificationChangeId!), viewModel.ProjectRecordId);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        // Load full organisation details for each selected record.
        var getOrganisationTasks = response.Content?
            .Select(o => rtsService.GetOrganisation(o.OrganisationId))
            .ToList() ?? [];

        var organisationsResponses = await Task.WhenAll(getOrganisationTasks);

        if (organisationsResponses.Any(r => !r.IsSuccessStatusCode))
        {
            return this.ServiceError(organisationsResponses.First(r => !r.IsSuccessStatusCode));
        }

        var organisations = organisationsResponses.Select(r => r.Content).ToList();

        viewModel.SelectedOrganisations = organisations.Adapt<List<ParticipatingOrganisationModel>>();

        // Map RTS organisation ID -> modification participating organisation ID for future delete operations.
        var dtoLookup = response.Content!.ToDictionary(x => x.OrganisationId, x => x.Id);

        foreach (var org in viewModel.SelectedOrganisations)
        {
            if (dtoLookup.TryGetValue(org.Id, out var orgId))
            {
                org.OrganisationId = orgId;
            }
        }

        TempData[TempDataKeys.ProjectModification.LinkBackToReferrer] = true;
        TempData[TempDataKeys.ProjectModification.UrlReferrer] = HttpContext.Request.GetDisplayUrl();
        TempData[TempDataKeys.ProjectModification.SelectedParticipatingOrganisations] = JsonSerializer.Serialize(viewModel.SelectedOrganisations);

        return View(viewModel);
    }

    /// <summary>
    /// Removes a selected organisation from the modification and redirects to the caller flow.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    public async Task<IActionResult> DeselectOrganisation(Guid organisationId, bool redirectToReview = false)
    {
        var baseModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());

        var response = await respondentService.DeleteModificationParticipatingOrganisation(organisationId);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        if (redirectToReview)
        {
            var reviewChangesParams = new Dictionary<string, string> {
                                { "projectRecordId", baseModel.ProjectRecordId },
                                { "specificAreaOfChangeId", baseModel.SpecificAreaOfChangeId! },
                                { "modificationChangeId", baseModel.ModificationChangeId! },
                                { "reviseChange", bool.TrueString },
                                };

            return RedirectToRoute("pmc:reviewchanges", reviewChangesParams);
        }

        return RedirectToAction(nameof(SelectedParticipatingOrganisations));
    }

    /// <summary>
    /// Displays the confirmation page for removing a participating organisation.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    public async Task<IActionResult> RemoveParticipatingOrganisation(Guid organisationId, string organisationName)
    {
        return View((organisationId, organisationName));
    }

    /// <summary>
    /// Clears all organisation search filters while keeping other search state available.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_ParticipatingOrganisations_Search)]
    [HttpGet]
    public IActionResult ClearFilters()
    {
        if (TempData[TempDataKeys.OrganisationSearchModel] is string orgSearchJson)
        {
            var orgSearchModel = JsonSerializer.Deserialize<OrganisationSearchModel>(orgSearchJson);

            if (orgSearchModel != null)
            {
                orgSearchModel.Countries.Clear();
                orgSearchModel.OrganisationStatuses.Clear();
                orgSearchModel.OrganisationTypes.Clear();

                TempData[TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(orgSearchModel);
            }
        }

        return RedirectToRoute("porgs:searchorganisation");
    }

    /// <summary>
    /// Removes a single filter chip/value from the persisted organisation search model.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_ParticipatingOrganisations_Search)]
    [HttpGet]
    public IActionResult RemoveFilter(string key, string? value)
    {
        if
        (
            TempData[TempDataKeys.OrganisationSearchModel] is not string orgSearchJson ||
            JsonSerializer.Deserialize<OrganisationSearchModel>(orgSearchJson) is not OrganisationSearchModel orgSearchModel
        )
        {
            return RedirectToAction(nameof(ParticipatingOrganisations));
        }

        switch (key)
        {
            case OrganisationSearch.CountryKey:
                if (!string.IsNullOrEmpty(value) && orgSearchModel.Countries.Count > 0)
                {
                    orgSearchModel.Countries =
                        [.. orgSearchModel.Countries.Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))];
                }

                break;

            case OrganisationSearch.OrganisationTypeKey:
                if (!string.IsNullOrEmpty(value) && orgSearchModel.OrganisationTypes.Count > 0)
                {
                    orgSearchModel.OrganisationTypes =
                        [.. orgSearchModel.OrganisationTypes.Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))];
                }

                break;

            case OrganisationSearch.OrganisationStatusKey:
                if (!string.IsNullOrEmpty(value) && orgSearchModel.OrganisationStatuses.Count > 0)
                {
                    orgSearchModel.OrganisationStatuses =
                        [.. orgSearchModel.OrganisationStatuses.Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))];
                }

                break;
        }

        TempData[TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(orgSearchModel);

        return RedirectToRoute("porgs:searchorganisation");
    }
}