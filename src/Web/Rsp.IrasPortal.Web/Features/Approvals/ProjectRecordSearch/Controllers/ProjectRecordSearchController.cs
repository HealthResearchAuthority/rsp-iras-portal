using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Extensions;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Approvals.ProjectRecordSearch.Models;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Approvals.ProjectRecordSearch.Controllers;

[Authorize(Policy = Workspaces.Approvals)]
[Route("approvals/[controller]/[action]", Name = "projectrecordsearch:[action]")]
public class ProjectRecordSearchController
(
    IApplicationsService applicationService,
    IRtsService rtsService
) : Controller
{
    [Authorize(Policy = Permissions.Approvals.ProjectRecords_Search)]
    [HttpGet("/approvals/[controller]", Name = "projectrecordsearch")]
    public async Task<IActionResult> Index
    (
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = "irasid",
        string? sortDirection = SortDirections.Ascending
    )
    {
        var userIsSystemAdmin = User.IsInRole(Roles.SystemAdministrator);

        var model = new ProjectRecordSearchViewModel();

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
            if (model.Search?.Filters?.Count == 0 && string.IsNullOrEmpty(model.Search.IrasId))
            {
                model.EmptySearchPerformed = true;
                return View(model);
            }

            var user = User;

            var searchQuery = new ProjectRecordSearchRequest()
            {
                IrasId = model.Search?.IrasId,
                FromDate = model.Search?.FromDate,
                ToDate = model.Search?.ToDate,
                ShortProjectTitle = model.Search?.ShortProjectTitle,
                ChiefInvestigatorName = model.Search?.ChiefInvestigatorName,
                LeadNation = model.Search?.LeadNation,
                ParticipatingNation = model.Search?.ParticipatingNation,
                SponsorOrganisation = model.Search?.SponsorOrganisation,
                ActiveProjectsOnly = !userIsSystemAdmin,
                AllowedStatuses = user.GetAllowedStatuses(StatusEntitiy.ProjectRecord),
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

            if (model.Search?.SponsorOrganisation != null)
            {
                // sponsor org filter is active
                // lookup the org name
                var activeOrg = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromOrganisationId(rtsService, model.Search.SponsorOrganisation);

                TempData[TempDataKeys.ActiveSponsoOrganisationFilterName] = activeOrg;
            }
        }

        return View(model);
    }

    [Authorize(Policy = Permissions.Approvals.ProjectRecords_Search)]
    [HttpPost]
    [CmsContentAction(nameof(Index))]
    public IActionResult ApplyFilters(ProjectRecordSearchViewModel model)
    {
        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.Approvals.ProjectRecords_Search)]
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

    [Authorize(Policy = Permissions.Approvals.ProjectRecords_Search)]
    [HttpGet]
    [CmsContentAction(nameof(Index))]
    public IActionResult RemoveFilter(string key, string? value)
    {
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Index));
        }

        var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;

        this.RemoveFilters(SessionKeys.ProjectRecordSearch, search, key, value);

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
        return await this.HandleOrganisationSearchAsync(
            rtsService,
            model,
            SessionKeys.ProjectRecordSearch,
            role,
            pageSize,
            pageIndex);
    }
}