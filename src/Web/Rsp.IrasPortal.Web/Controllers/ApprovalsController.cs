using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "approvals:[action]")]
[Authorize(Roles = "system_administrator,workflow_co-ordinator,team_manager,study-wide_reviewer")]
public class ApprovalsController
(
    IApplicationsService applicationsService,
    IRtsService rtsService,
    IValidator<ApprovalsSearchModel> validator
) : Controller
{
    [Route("/approvals", Name = "approvals:welcome")]
    public IActionResult Welcome()
    {
        TempData.Remove(TempDataKeys.ApprovalsSearchModel);
        return View(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Search
    (
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ModificationsModel.ModificationId),
        string? sortDirection = SortDirections.Descending
    )
    {
        var model = new ApprovalsSearchViewModel();

        if (TempData.ContainsKey(TempDataKeys.ApprovalsSearchModel))
        {
            var json = TempData.Peek(TempDataKeys.ApprovalsSearchModel)?.ToString();
            if (!string.IsNullOrEmpty(json))
            {
                var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
                model.Search = search;

                if (search.Filters.Count == 0 && string.IsNullOrEmpty(search.IrasId))
                {
                    model.EmptySearchPerformed = true;
                    return View(model);
                }

                var searchQuery = new ModificationSearchRequest
                {
                    IrasId = search.IrasId,
                    ChiefInvestigatorName = search.ChiefInvestigatorName,
                    LeadNation = search.LeadNation,
                    ParticipatingNation = search.ParticipatingNation,
                    FromDate = search.FromDate,
                    ToDate = search.ToDate,
                    ModificationTypes = search.ModificationTypes,
                    ShortProjectTitle = search.ShortProjectTitle,
                    SponsorOrganisation = search.SponsorOrganisation,
                };

                var result = await applicationsService.GetModifications(searchQuery, pageNumber, pageSize, sortField, sortDirection);

                model.Modifications = result?.Content?.Modifications?
                    .Select(dto => new ModificationsModel
                    {
                        ModificationId = dto.ModificationId,
                        ShortProjectTitle = dto.ShortProjectTitle,
                        ModificationType = dto.ModificationType,
                        ChiefInvestigator = dto.ChiefInvestigator,
                        LeadNation = dto.LeadNation,
                        SponsorOrganisation = dto.SponsorOrganisation,
                        CreatedAt = dto.CreatedAt
                    })
                    .ToList() ?? [];

                model.Pagination = new PaginationViewModel(pageNumber, pageSize, result?.Content?.TotalCount ?? 0)
                {
                    SortDirection = sortDirection,
                    SortField = sortField
                };
            }
        }

        model.SortField = sortField;
        model.SortDirection = sortDirection;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ApplyFilters(ApprovalsSearchViewModel model)
    {
        var validationResult = await validator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(Search), model);
        }

        TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(model.Search);
        return RedirectToAction(nameof(Search));
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        if (!TempData.TryGetValue(TempDataKeys.ApprovalsSearchModel, out var tempDataValue))
        {
            return RedirectToAction(nameof(Search));
        }

        var json = tempDataValue?.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Search));
        }

        var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json);
        if (search == null)
        {
            return RedirectToAction(nameof(Search));
        }

        // Retain only the IRAS Project ID
        var cleanedSearch = new ApprovalsSearchModel
        {
            IrasId = search.IrasId
        };

        TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(cleanedSearch);

        return RedirectToAction(nameof(Search));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key, string? value)
    {
        if (!TempData.TryGetValue(TempDataKeys.ApprovalsSearchModel, out var tempDataValue))
        {
            return RedirectToAction(nameof(Search));
        }

        var json = tempDataValue?.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Search));
        }

        var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;

        var keyNormalized = key?.ToLowerInvariant().Replace(" ", "");

        switch (keyNormalized)
        {
            case "chiefinvestigatorname":
                search.ChiefInvestigatorName = null;
                break;

            case "shortprojecttitle":
                search.ShortProjectTitle = null;
                break;

            case "sponsororganisation":
                search.SponsorOrganisation = null;
                search.SponsorOrgSearch = new OrganisationSearchViewModel();
                break;

            case "datesubmitted":
                search.FromDay = search.FromMonth = search.FromYear = null;
                search.ToDay = search.ToMonth = search.ToYear = null;
                break;

            case "datesubmitted-from":
                search.FromDay = search.FromMonth = search.FromYear = null;
                break;

            case "datesubmitted-to":
                search.ToDay = search.ToMonth = search.ToYear = null;
                break;

            case "leadnation":
                if (!string.IsNullOrEmpty(value) && search.LeadNation?.Count > 0)
                {
                    search.LeadNation = search.LeadNation
                        .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                break;

            case "participatingnation":
                if (!string.IsNullOrEmpty(value) && search.ParticipatingNation?.Count > 0)
                {
                    search.ParticipatingNation = search.ParticipatingNation
                        .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                break;

            case "modificationtype":
                if (!string.IsNullOrEmpty(value) && search.ModificationTypes?.Count > 0)
                {
                    search.ModificationTypes = search.ModificationTypes
                        .Where(m => !string.Equals(m, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                break;
        }

        // Write the updated search model back to TempData
        TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(search);

        return await ApplyFilters(new ApprovalsSearchViewModel { Search = search });
    }

    /// <summary>
    ///     Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination. Defults to 5 if not provided.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> SearchOrganisations(ApprovalsSearchViewModel model, string? role, int? pageSize = 5, int pageIndex = 1)
    {
        var returnUrl = TempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        // store the irasId in the TempData to get in the view
        TempData.TryAdd(TempDataKeys.IrasId, model.Search.IrasId);

        // set the previous, current and next stages
        TempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");

        // when search is performed, empty the currently selected organisation
        model.Search.SponsorOrgSearch.SelectedOrganisation = string.Empty;

        // add the search model to temp data to use in the view
        TempData.TryAdd(TempDataKeys.OrgSearch, model.Search.SponsorOrgSearch, true);

        if (string.IsNullOrEmpty(model.Search.SponsorOrgSearch.SearchText) || model.Search.SponsorOrgSearch.SearchText.Length < 3)
        {
            // add model validation error if search text is empty
            ModelState.AddModelError("sponsor_org_search",
                "Please provide 3 or more characters to search sponsor organisation.");

            // save the model state in temp data, to use it on redirects to show validation errors
            // the modelstate will be merged using the action filter ModelStateMergeAttribute
            // only if the TempData has ModelState stored
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);

            // Return the view with the model state errors.
            return Redirect(returnUrl!);
        }

        // Use the default sponsor role if no role is provided.
        role ??= OrganisationRoles.Sponsor;

        // Fetch organisations from the RTS service, with or without pagination.
        var searchResponse = await rtsService.GetOrganisationsByName(model.Search.SponsorOrgSearch.SearchText, role, pageIndex, pageSize);

        // Handle error response from the service.
        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return this.ServiceError(searchResponse);
        }

        // Convert the response content to a list of organisation names.
        var sponsorOrganisations = searchResponse.Content;

        TempData.TryAdd(TempDataKeys.SponsorOrganisations, sponsorOrganisations, true);

        TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(model.Search);

        return Redirect(returnUrl!);
    }
}