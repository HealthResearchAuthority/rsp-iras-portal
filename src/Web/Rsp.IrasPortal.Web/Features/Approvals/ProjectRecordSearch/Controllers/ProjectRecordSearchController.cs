using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Approvals.ProjectRecord.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Approvals.ProjectRecord.Controllers;

[Route("[controller]/[action]", Name = "projectrecordsearch:[action]")]
[Authorize]
public class ProjectRecordSearchController(
    IApplicationsService applicationService,
    IRtsService rtsService
    ) : Controller
{
    [HttpGet("~/[controller]", Name = "projectrecordsearch")]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ApplicationModel.CreatedDate),
        string? sortDirection = SortDirections.Descending)
    {
        var model = new ProjectRecordSearchViewModel();

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
            if (model.Search?.Filters?.Count == 0 && string.IsNullOrEmpty(model.Search.IrasId))
            {
                model.EmptySearchPerformed = true;
                return View("~/Features/Approvals/ProjectRecordSearch/Views/Index.cshtml", model);
            }

            var searchQuery = new ProjectRecordSearchRequest()
            {
                IrasId = model.Search?.IrasId,
                FromDate = model.Search?.FromDate,
                ToDate = model.Search?.ToDate,
                ShortProjectTitle = model.Search?.ShortProjectTitle,
                ChiefInvestigatorName = model.Search?.ChiefInvestigatorName,
                LeadNation = model.Search?.LeadNation,
                ParticipatingNation = model.Search?.ParticipatingNation,
                SponsorOrganisation = model.Search?.SponsorOrganisation
            };

            var applicationServiceResponse = await applicationService.GetPaginatedApplications(
                searchQuery,
                pageNumber,
                pageSize,
                sortField,
                sortDirection
                );

            model.Applications = applicationServiceResponse?.Content?.Items ?? [];

            model.Pagination = new PaginationViewModel(pageNumber, pageSize, applicationServiceResponse?.Content?.TotalCount ?? 0)
            {
                RouteName = "projectrecordsearch",
                SortDirection = sortDirection,
                SortField = sortField,
                FormName = "applications-selection"
            };
        }

        return View("~/Features/Approvals/ProjectRecordSearch/Views/Index.cshtml", model);
    }

    [HttpPost]
    [CmsContentAction(nameof(Index))]
    public IActionResult ApplyFilters(ProjectRecordSearchViewModel model)
    {
        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [CmsContentAction(nameof(Index))]
    public IActionResult ClearFilters()
    {
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Index));
        }

        var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json);
        if (search == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // Retain only the IRAS Project ID
        var cleanedSearch = new ApprovalsSearchModel
        {
            IrasId = search.IrasId
        };

        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(cleanedSearch));

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [CmsContentAction(nameof(Index))]
    public async Task<IActionResult> RemoveFilter(string key, string? value)
    {
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Index));
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
        }

        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(search));

        return ApplyFilters(new ProjectRecordSearchViewModel { Search = search });
    }

    /// <summary>
    ///     Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination. Defults to 5 if not provided.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    [CmsContentAction(nameof(Index))]
    public async Task<IActionResult> SearchOrganisations(ApprovalsSearchViewModel model, string? role, int? pageSize = 5, int pageIndex = 1)
    {
        var returnUrl = TempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        // store the irasId in the TempData to get in the view
        TempData.TryAdd(TempDataKeys.IrasId, model.Search.IrasId);

        // set the previous, current and next stages
        TempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");

        // when search is performed, empty the currently selected organisation
        model.Search.SponsorOrgSearch.SelectedOrganisation = string.Empty;
        TempData.TryAdd(TempDataKeys.OrgSearch, model.Search.SponsorOrgSearch, true);

        if (string.IsNullOrEmpty(model.Search.SponsorOrgSearch.SearchText) || model.Search.SponsorOrgSearch.SearchText.Length < 3)
        {
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

        var searchResponse = await rtsService.GetOrganisationsByName(model.Search.SponsorOrgSearch.SearchText, role, pageIndex, pageSize);

        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return this.ServiceError(searchResponse);
        }

        var sponsorOrganisations = searchResponse.Content;

        TempData.TryAdd(TempDataKeys.SponsorOrganisations, sponsorOrganisations, true);

        //THIS IS ONLY USED HERE TO NOT SHOW THE FILTERS IF WE RUN A NON JAVASCRIPT ORG SEARCH
        model.Search.IgnoreFilters = true;

        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(model.Search));

        return Redirect(returnUrl!);
    }
}