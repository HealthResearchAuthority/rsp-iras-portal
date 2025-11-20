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
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "approvals:[action]")]
[Authorize(Roles = "system_administrator,workflow_co-ordinator,team_manager,study-wide_reviewer")]
public class ApprovalsController
(
    IProjectModificationsService projectModificationsService,
    IRtsService rtsService,
    IValidator<ApprovalsSearchModel> validator
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index
    (
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsModel.ModificationId),
        string sortDirection = SortDirections.Descending
    )
    {
        var model = new ApprovalsSearchViewModel();

        var json = HttpContext.Session.GetString(SessionKeys.ApprovalsSearch);
        if (!string.IsNullOrEmpty(json))
        {
            var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
            model.Search = search;

            if (search.Filters.Count == 0 && string.IsNullOrEmpty(search.IrasId))
            {
                model.EmptySearchPerformed = true;
                return View(model);
            }

            ViewBag.DisplayName = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromOrganisationId(rtsService, search.SponsorOrganisation);

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
                IncludeReviewerId = false
            };

            if (User.IsInRole(Roles.TeamManager) || User.IsInRole(Roles.StudyWideReviewer) || User.IsInRole(Roles.WorkflowCoordinator))
            {
                searchQuery.AllowedStatuses.Add(ModificationStatus.Approved);
                searchQuery.AllowedStatuses.Add(ModificationStatus.NotApproved);
                searchQuery.AllowedStatuses.Add(ModificationStatus.WithReviewBody);
            }
            if (User.IsInRole(Roles.SystemAdministrator))
            {
                // ALLOW ALL STATUS
                searchQuery.AllowedStatuses = [];
            }

            var result = await projectModificationsService.GetModifications(searchQuery, pageNumber, pageSize, sortField, sortDirection);

            model.Modifications = result?.Content?.Modifications?
                .Select(dto => new ModificationsModel
                {
                    Id = dto.Id,
                    ModificationId = dto.ModificationId,
                    ShortProjectTitle = dto.ShortProjectTitle,
                    ModificationType = dto.ModificationType,
                    ChiefInvestigator = dto.ChiefInvestigator,
                    LeadNation = dto.LeadNation,
                    SponsorOrganisation = dto.SponsorOrganisation,
                    CreatedAt = dto.CreatedAt,
                    ProjectRecordId = dto.ProjectRecordId,
                    Status =
                        !string.IsNullOrWhiteSpace(dto.ReviewerName) && dto.Status == ModificationStatus.WithReviewBody
                            ? ModificationStatus.ReviewInProgress
                            : dto.Status == ModificationStatus.WithReviewBody
                                ? ModificationStatus.Received
                                : dto.Status,
                })
                .ToList() ?? [];

            model.Pagination = new PaginationViewModel(pageNumber, pageSize, result?.Content?.TotalCount ?? 0)
            {
                SortDirection = sortDirection,
                SortField = sortField
            };
        }

        return View(model);
    }

    [HttpPost]
    [CmsContentAction(nameof(Index))]
    public async Task<IActionResult> ApplyFilters(ApprovalsSearchViewModel model)
    {
        var validationResult = await validator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(Index), model);
        }

        HttpContext.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        var json = HttpContext.Session.GetString(SessionKeys.ApprovalsSearch);
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

        HttpContext.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(cleanedSearch));

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key, string? value)
    {
        var json = HttpContext.Session.GetString(SessionKeys.ApprovalsSearch);
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Index));
        }

        var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;

        this.RemoveFilters(SessionKeys.ApprovalsSearch, search, key, value);

        HttpContext.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(search));

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
        return await this.HandleOrganisationSearchAsync(
            rtsService,
            model,
            SessionKeys.ApprovalsSearch,
            role,
            pageSize,
            pageIndex);
    }
}