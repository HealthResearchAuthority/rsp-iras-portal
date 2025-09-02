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

[Route("[controller]/[action]", Name = "mytasklist:[action]")]
[Authorize(Roles = "study-wide_reviewer")]
public class MyTasklistController(IApplicationsService applicationsService, IValidator<ApprovalsSearchModel> validator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        List<string>? selectedModificationIds = null,
        string? sortField = nameof(ModificationsModel.CreatedAt),
        string? sortDirection = SortDirections.Ascending)
    {
        var model = new MyTasklistViewModel
        {
            SelectedModificationIds = selectedModificationIds ?? [],
            EmptySearchPerformed = true // Set to true to check if search bar should be hidden on view
        };

        var json = HttpContext.Session.GetString(SessionKeys.MyTasklist);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
            if (model.Search.Filters.Count != 0 || !string.IsNullOrEmpty(model.Search.IrasId))
            {
                model.EmptySearchPerformed = false;
            }
        }

        var searchQuery = new ModificationSearchRequest()
        {
            LeadNation = ["England"],
            ShortProjectTitle = model.Search.ShortProjectTitle,
            FromDate = model.Search.FromDate,
            ToDate = model.Search.ToDate,
            IrasId = model.Search.IrasId,
        };

        // Since we are searching backwards from the current date, we need to reverse the logic for the date range.
        if (model.Search.FromSubmission != null)
        {
            var fromDaysSinceSubmission = DateTime.UtcNow.AddDays(-model.Search.FromSubmission.Value);
            searchQuery.ToDate = fromDaysSinceSubmission;
        }
        if (model.Search.ToSubmission != null)
        {
            var toDaysSinceSubmisison = DateTime.UtcNow.AddDays(-model.Search.ToSubmission.Value);
            toDaysSinceSubmisison = toDaysSinceSubmisison.AddDays(-1).AddTicks(1);
            searchQuery.FromDate = toDaysSinceSubmisison;
        }

        var querySortField = sortField;
        var querySortDirection = sortDirection;

        if (sortField == nameof(ModificationsModel.DaysSinceSubmission))
        {
            querySortField = nameof(ModificationsModel.CreatedAt);
            querySortDirection = sortDirection == SortDirections.Ascending
                ? SortDirections.Descending
                : SortDirections.Ascending;
        }

        var result = await applicationsService.GetMyTasklistModifications(
            searchQuery, pageNumber, pageSize, querySortField, querySortDirection);

        model.Modifications = result?.Content?.Modifications?
            .Select(dto => new TaskListModificationViewModel
            {
                Modification = new ModificationsModel
                {
                    ModificationId = dto.ModificationId,
                    ShortProjectTitle = dto.ShortProjectTitle,
                    ModificationType = dto.ModificationType,
                    ChiefInvestigator = dto.ChiefInvestigator,
                    LeadNation = dto.LeadNation,
                    SponsorOrganisation = dto.SponsorOrganisation,
                    CreatedAt = dto.CreatedAt
                },
                IsSelected = selectedModificationIds?.Contains(dto.ModificationId) == true
            })
            .ToList() ?? [];

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, result?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField,
        };

        return View(model);
    }



    [HttpPost]
    public async Task<IActionResult> ApplyFilters(MyTasklistViewModel model)
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

        HttpContext.Session.SetString(SessionKeys.MyTasklist, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key)
    {
        var json = HttpContext.Session.GetString(SessionKeys.MyTasklist);
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Index));
        }

        var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;

        switch (key?.ToLowerInvariant().Replace(" ", ""))
        {
            case "shortprojecttitle":
                search.ShortProjectTitle = null;
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

            case "dayssincesubmission-from":
                search.FromDaysSinceSubmission = null;
                break;

            case "dayssincesubmission-to":
                search.ToDaysSinceSubmission = null;
                break;
        }

        HttpContext.Session.SetString(SessionKeys.MyTasklist, JsonSerializer.Serialize(search));

        return await ApplyFilters(new MyTasklistViewModel { Search = search });
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        var json = HttpContext.Session.GetString(SessionKeys.MyTasklist);
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToAction(nameof(Index));
        }

        var search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json);
        if (search == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var cleanedSearch = new ApprovalsSearchModel
        {
            IrasId = search.IrasId
        };

        HttpContext.Session.SetString(SessionKeys.MyTasklist,
            JsonSerializer.Serialize(cleanedSearch));

        return RedirectToAction(nameof(Index));
    }
}