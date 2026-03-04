using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> SearchOrganisation(SearchOrganisationViewModel model)
    {
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

        var response = await rtsService.SearchOrganisations(organisationsSearchRequest, 1, 10);

        viewModel.Organisations = response?.Content?.Organisations?.Select(dto => new SelectableOrganisationViewModel
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

        viewModel.Pagination = new PaginationViewModel(1, 10, response?.Content?.TotalCount ?? 0)
        {
            SortDirection = SortDirections.Ascending,
            SortField = nameof(OrganisationModel.Name),
            FormName = "organisation-selection"
        };

        return View(viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.Modifications_Update)]
    [HttpPost]
    public IActionResult SaveSelection(bool saveForLater)
    {
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;
        var status = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationStatus) as string ?? string.Empty;
        var sponsorOrganisationUserId = TempData.Peek(TempDataKeys.RevisionSponsorOrganisationUserId);
        var rtsId = TempData.Peek(TempDataKeys.RevisionRtsId) as string ?? string.Empty;

        if (saveForLater)
        {
            TempData[TempDataKeys.ShowNotificationBanner] = true;
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();
            if (status is ModificationStatus.ReviseAndAuthorise)
            {
                return RedirectToRoute("sws:modifications", new { sponsorOrganisationUserId, rtsId });
            }

            return RedirectToRoute("pov:postapproval", new { projectRecordId });
        }
        else
        {
            return RedirectToAction("ParticipatingOrganisation");
        }
    }

    [HttpPost]
    public IActionResult ConfirmSelection(List<SelectableOrganisationViewModel> Organisations)
    {
        var selectedOrganisations = Organisations.Where(o => o.IsSelected).Select(o => o.Organisation).ToList();

        var model = new SelectedOrganisationViewModel
        {
            Organisations = selectedOrganisations,
            Pagination = new PaginationViewModel(1, 10, selectedOrganisations.Count)
            {
                SortDirection = SortDirections.Ascending,
                SortField = nameof(OrganisationModel.Name),
                FormName = "selected-organisations"
            }
        };

        return View("SelectedParticipatingOrganisations", model);

        //var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;
        //var status = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationStatus) as string ?? string.Empty;
        //var sponsorOrganisationUserId = TempData.Peek(TempDataKeys.RevisionSponsorOrganisationUserId);
        //var rtsId = TempData.Peek(TempDataKeys.RevisionRtsId) as string ?? string.Empty;

        //if (saveForLater)
        //{
        //    TempData[TempDataKeys.ShowNotificationBanner] = true;
        //    TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();
        //    if (status is ModificationStatus.ReviseAndAuthorise)
        //    {
        //        return RedirectToRoute("sws:modifications", new { sponsorOrganisationUserId, rtsId });
        //    }

        //    return RedirectToRoute("pov:postapproval", new { projectRecordId });
        //}
        //else
        //{
        //    return RedirectToAction("ParticipatingOrganisation");
        //}
    }

    //[HttpGet]
    //public IActionResult ClearFilters([FromQuery] string? searchQuery = null)
    //{
    //    var cleanedSearch = new OrganisationSearchModel
    //    {
    //        SearchNameTerm = searchQuery
    //    };

    //    // Clear any saved filters from session
    //    HttpContext.Session.Remove(SessionKeys.OrganisationsSearch);

    //    // Save the current search filters to the session
    //    HttpContext.Session.SetString(SessionKeys.OrganisationsSearch, JsonSerializer.Serialize(cleanedSearch));

    //    return RedirectToRoute("pmc:searchorganisation", new
    //    {
    //        pageNumber = 1,
    //        pageSize = 10
    //    });
    //}

    //[HttpGet]
    //[Route("/admin/users/removefilter", Name = "admin:removefilter")]
    //public IActionResult RemoveFilter(string key, string? value)
    //{
    //    var json = HttpContext.Session.GetString(SessionKeys.UsersSearch);
    //    if (string.IsNullOrWhiteSpace(json))
    //    {
    //        return RedirectToAction(nameof(Index));
    //    }
    //    var viewModel = JsonSerializer.Deserialize<UserSearchModel>(json)!;

    //    switch (key)
    //    {
    //        case UsersSearch.CountryKey:
    //            if (!string.IsNullOrEmpty(value) && viewModel.Country != null)
    //            {
    //                viewModel.Country = viewModel.Country
    //                    .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
    //                    .ToList();
    //            }

    //            break;

    //        case UsersSearch.FromDateKey:
    //            viewModel.FromDay = viewModel.FromMonth = viewModel.FromYear = null;
    //            break;

    //        case UsersSearch.ToDateKey:
    //            viewModel.ToDay = viewModel.ToMonth = viewModel.ToYear = null;
    //            break;

    //        case UsersSearch.StatusKey:
    //            viewModel.Status = null;
    //            break;
    //        // NEW: turn off selected Review Bodies
    //        case UsersSearch.ReviewBodyKey:
    //            {
    //                // if value is empty -> clear all selections
    //                if (string.IsNullOrWhiteSpace(value))
    //                {
    //                    foreach (var rb in viewModel.ReviewBodies) rb.IsSelected = false;
    //                    break;
    //                }

    //                var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    //                foreach (var token in tokens)
    //                {
    //                    if (Guid.TryParse(token, out var id))
    //                    {
    //                        foreach (var rb in viewModel.ReviewBodies.Where(x => x.Id == id))
    //                            rb.IsSelected = false;
    //                    }
    //                    else
    //                    {
    //                        foreach (var rb in viewModel.ReviewBodies.Where(x =>
    //                                 string.Equals(x.RegulatoryBodyName, token, StringComparison.OrdinalIgnoreCase) ||
    //                                 string.Equals(x.DisplayName, token, StringComparison.OrdinalIgnoreCase)))
    //                            rb.IsSelected = false;
    //                    }
    //                }

    //                break;
    //            }

    //        // NEW: turn off selected Roles
    //        case UsersSearch.RoleKey:
    //            {
    //                foreach (var r in viewModel.UserRoles.Where(x => x.DisplayName == value))
    //                {
    //                    r.IsSelected = false;
    //                }
    //            }
    //            break;
    //    }

    //    // Save applied filters to session
    //    HttpContext.Session.SetString(SessionKeys.UsersSearch, JsonSerializer.Serialize(viewModel));

    //    // Redirect to ViewReviewBodies with query parameters
    //    return RedirectToRoute("admin:users", new
    //    {
    //        pageNumber = 1,
    //        pageSize = 20,
    //        fromPagination = true
    //    });
    //}
}