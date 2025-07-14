using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

[ExcludeFromCodeCoverage]
[Route("[controller]/[action]", Name = "approvals:[action]")]
[Authorize(Policy = "IsUser")]
public class ApprovalsController(
    IApplicationsService applicationsService,
    IRtsService rtsService,
    IValidator<ApprovalsSearchModel> validator
) : Controller
{
    private const string TempDataKey_ApprovalsSearch = "ApprovalsSearchModel";

    [Route("/approvals", Name = "approvals:welcome")]
    public IActionResult Welcome()
    {
        TempData.Remove(TempDataKey_ApprovalsSearch);
        return View(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Search(int pageNumber = 1, int pageSize = 20)
    {
        var model = new ApprovalsSearchViewModel();

        if (TempData.ContainsKey(TempDataKey_ApprovalsSearch))
        {
            var json = TempData.Peek(TempDataKey_ApprovalsSearch)?.ToString();
            if (!string.IsNullOrEmpty(json))
            {
                var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
                model.Search = search;

                if (search.Filters.Count == 0)
                {
                    model.EmptySearchPerformed = true;
                    return View(model);
                }

                var searchQuery = new ModificationSearchRequest
                {
                    IrasId = search.IrasId,
                    ChiefInvestigatorName = search.ChiefInvestigatorName,
                    Country = search.Country,
                    FromDate = search.FromDate,
                    ToDate = search.ToDate,
                    ModificationTypes = search.ModificationTypes,
                    ShortProjectTitle = search.ShortProjectTitle,
                    SponsorOrganisation = search.SponsorOrganisation
                };

                var result = await applicationsService.GetModifications(searchQuery, pageNumber, pageSize);

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

                model.Pagination = new PaginationViewModel(pageNumber, pageSize, result?.Content?.TotalCount ?? 0);
            }
        }

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

        TempData[TempDataKey_ApprovalsSearch] = JsonSerializer.Serialize(model.Search);
        return RedirectToAction(nameof(Search));
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        TempData.Remove(TempDataKey_ApprovalsSearch);
        return RedirectToAction(nameof(Search));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key, string? value)
    {
        if (!TempData.TryGetValue(TempDataKey_ApprovalsSearch, out var tempDataValue))
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

            case "projecttitle":
                search.ShortProjectTitle = null;
                break;

            case "sponsororganisation":
                search.SponsorOrganisation = null;
                search.SponsorOrgSearch = new OrganisationSearchViewModel();
                break;

            case "fromdate":
                search.FromDay = search.FromMonth = search.FromYear = null;
                break;

            case "todate":
                search.ToDay = search.ToMonth = search.ToYear = null;
                break;

            case "leadnation":
                if (!string.IsNullOrEmpty(value) && search.Country?.Count > 0)
                {
                    search.Country = search.Country
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
        TempData[TempDataKey_ApprovalsSearch] = JsonSerializer.Serialize(search);

        return await ApplyFilters(new ApprovalsSearchViewModel { Search = search });
    }


    /// <summary>
    ///     Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> SearchOrganisations(ApprovalsSearchViewModel model, string? role, int? pageSize)
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
        var searchResponse = pageSize is null
            ? await rtsService.GetOrganisations(model.Search.SponsorOrgSearch.SearchText!, role)
            : await rtsService.GetOrganisations(model.Search.SponsorOrgSearch.SearchText, role, pageSize.Value);

        // Handle error response from the service.
        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return this.ServiceError(searchResponse);
        }

        // Convert the response content to a list of organisation names.
        var sponsorOrganisations = searchResponse.Content;

        TempData.TryAdd(TempDataKeys.SponsorOrganisations, sponsorOrganisations, true);

        TempData[TempDataKey_ApprovalsSearch] = JsonSerializer.Serialize(model.Search);

        return Redirect(returnUrl!);
    }
}