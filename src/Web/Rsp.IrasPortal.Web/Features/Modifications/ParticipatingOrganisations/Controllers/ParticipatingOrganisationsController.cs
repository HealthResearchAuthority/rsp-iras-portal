using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
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
    /// Returns the view for selecting a participating organisation.
    /// Populates metadata from TempData.
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpGet("/modifications/participatingorganisations", Name = "pmc:[action]")]
    public async Task<IActionResult> ParticipatingOrganisation()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new SearchOrganisationViewModel());

        return View(nameof(SearchOrganisation), viewModel);
    }

    /// <summary>
    /// Handles search form submission for participant organisation lookup.
    /// Validates the search model and returns the view with errors if needed.
    /// </summary>
    public async Task<IActionResult> SearchOrganisation
    (
        int pageNumber = 1,
        int pageSize = 10,
        [FromForm] SearchOrganisationViewModel? model = null,
        string? sortField = nameof(OrganisationModel.Name),
        string? sortDirection = SortDirections.Ascending
    )
    {
        // see if the model was posted back with search criteria
        model ??= new SearchOrganisationViewModel();
        model.Search ??= new OrganisationSearchModel();
        model.Pagination ??= new PaginationViewModel(pageNumber, pageSize, 0)
        {
            SortDirection = sortDirection!.ToLower(),
            SortField = sortField!.ToLower(),
            FormName = "organisation-selection"
        };

        // if model was not posted, try to populate from TempData (e.g. from filters or paging), if not, create new
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

        var organisationsSearchRequest = model.Search.Adapt<OrganisationsSearchRequest>();

        var response = await rtsService.SearchOrganisations(organisationsSearchRequest, pageNumber, pageSize, sortDirection!, sortField!);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        // get the existing selected organisations for the selected area of change
        var existingOrgsResponse = await respondentService
            .GetModificationParticipatingOrganisationsBySpecificArea(viewModel.ProjectRecordId, viewModel.SpecificAreaOfChangeId!);

        if (!existingOrgsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(existingOrgsResponse);
        }

        var selectedOrganisationsIds = existingOrgsResponse.Content?.Select(o => o.OrganisationId).ToList() ?? [];

        response.Content!.Organisations = [.. response.Content!.Organisations.ExceptBy(selectedOrganisationsIds, o => o.Id)];

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

        viewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, response.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection!.ToLower(),
            SortField = sortField!.ToLower(),
            FormName = "organisation-selection"
        };

        TempData[TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(model.Search);
        TempData[TempDataKeys.OrganisationSearchResults] = JsonSerializer.Serialize(viewModel);

        return View(viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpPost]
    public async Task<IActionResult> ConfirmSelection(SearchOrganisationViewModel model, bool saveForLater, string sortField = "name", string sortDirection = "asc")
    {
        var searchOrganisationViewModel = TempData.PopulateBaseProjectModificationProperties(model);

        // if no organisations were selected, add model error and return to search results view with existing data (either from TempData or current search)
        if (!saveForLater && searchOrganisationViewModel.Organisations?.Any(org => org.IsSelected) is not true)
        {
            ModelState.AddModelError("participating-organisations", "Please select at least one organisation.");

            // repopulate pagination from TempData if available, otherwise use current search results
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

        if (selectedOrganisations.Count == 0)
        {
            return RedirectToAction(nameof(ParticipatingOrganisation));
        }

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

        if (saveForLater)
        {
            var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;
            var rtsId = TempData.Peek(TempDataKeys.RevisionRtsId) as string ?? string.Empty;
            var status = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationStatus) as string ?? string.Empty;
            var sponsorOrganisationUserId = TempData.Peek(TempDataKeys.RevisionSponsorOrganisationUserId);

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

    [HttpGet]
    public async Task<IActionResult> SelectedParticipatingOrganisations()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new SelectedOrganisationsViewModel());

        var response = await respondentService.GetModificationParticipatingOrganisations(Guid.Parse(viewModel.ModificationChangeId!), viewModel.ProjectRecordId);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

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

        var dtoLookup = response.Content!.ToDictionary(x => x.OrganisationId, x => x.Id);

        foreach (var org in viewModel.SelectedOrganisations)
        {
            if (dtoLookup.TryGetValue(org.Id, out var orgId))
            {
                org.OrganisationId = orgId;
            }
        }

        TempData[TempDataKeys.ProjectModification.SelectedParticipatingOrganisations] = JsonSerializer.Serialize(viewModel.SelectedOrganisations);

        return View(viewModel);
    }

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

    public async Task<IActionResult> RemoveParticipatingOrganisation(Guid organisationId, string organisationName)
    {
        return View((organisationId, organisationName));
    }

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

    [HttpGet]
    public IActionResult RemoveFilter(string key, string? value)
    {
        if
        (
            TempData[TempDataKeys.OrganisationSearchModel] is not string orgSearchJson ||
            JsonSerializer.Deserialize<OrganisationSearchModel>(orgSearchJson) is not OrganisationSearchModel orgSearchModel
        )
        {
            return RedirectToAction(nameof(ParticipatingOrganisation));
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