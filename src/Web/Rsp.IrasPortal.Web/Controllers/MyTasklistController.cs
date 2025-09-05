using System.Security.Claims;
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
public class MyTasklistController(IProjectModificationsService projectModificationsService, IValidator<ApprovalsSearchModel> validator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        List<string>? selectedModificationIds = null,
        string? sortField = nameof(ModificationsModel.CreatedAt),
        string? sortDirection = SortDirections.Ascending)
    {
        var json = HttpContext.Session.GetString(SessionKeys.MyTasklist);

        var search = string.IsNullOrWhiteSpace(json)
            ? new ApprovalsSearchModel()
            : JsonSerializer.Deserialize<ApprovalsSearchModel>(json) ?? new();

        var model = new MyTasklistViewModel
        {
            Search = search,
            EmptySearchPerformed = (search.Filters?.Count ?? 0) == 0 && string.IsNullOrEmpty(search.IrasId)
        };

        var searchQuery = new ModificationSearchRequest
        {
            LeadNation = ["England"],
            FromDate = search.FromDate,
            ToDate = search.ToDate,
            IrasId = search.IrasId,
            ReviewerId = User?.FindFirst("userId")?.Value
        };

        // Reverse date logic when searching by "days since submission"
        if (search.FromSubmission is int fromSub)
            searchQuery.ToDate = DateTime.UtcNow.AddDays(-fromSub);

        if (search.ToSubmission is int toSub)
            searchQuery.FromDate = DateTime.UtcNow.AddDays(-toSub).AddDays(-1).AddTicks(1);

        // Map sort for DaysSinceSubmission -> CreatedAt with flipped direction
        (string qSortField, string qSortDir) =
            sortField == nameof(ModificationsModel.DaysSinceSubmission)
                ? (nameof(ModificationsModel.CreatedAt),
                   sortDirection == SortDirections.Ascending ? SortDirections.Descending : SortDirections.Ascending)
                : (sortField!, sortDirection!);

        var result = await projectModificationsService.GetModifications(
            searchQuery, pageNumber, pageSize, qSortField, qSortDir);

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
            }).ToList() ?? new();

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, result?.Content?.TotalCount ?? 0)
        {
            SortField = sortField!,
            SortDirection = sortDirection!
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
        if (string.IsNullOrWhiteSpace(json) ||
            JsonSerializer.Deserialize<ApprovalsSearchModel>(json) is not { } search)
            return RedirectToAction(nameof(Index));

        var k = key?.ToLowerInvariant().Replace(" ", "");

        void ClearFromDate() => search.FromDay = search.FromMonth = search.FromYear = null;
        void ClearToDate() => search.ToDay = search.ToMonth = search.ToYear = null;

        var actions = new Dictionary<string, Action>(StringComparer.Ordinal)
        {
            ["datesubmitted"] = () => { ClearFromDate(); ClearToDate(); },
            ["datesubmitted-from"] = ClearFromDate,
            ["datesubmitted-to"] = ClearToDate,
            ["dayssincesubmission-from"] = () => search.FromDaysSinceSubmission = null,
            ["dayssincesubmission-to"] = () => search.ToDaysSinceSubmission = null,
            ["shortprojecttitle"] = () => search.ShortProjectTitle = null
        };

        if (k is not null && actions.TryGetValue(k, out var act))
            act();

        HttpContext.Session.SetString(SessionKeys.MyTasklist, JsonSerializer.Serialize(search));
        return await ApplyFilters(new MyTasklistViewModel { Search = search });
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        var json = HttpContext.Session.GetString(SessionKeys.MyTasklist);
        if (string.IsNullOrWhiteSpace(json))
            return RedirectToAction(nameof(Index));

        if (JsonSerializer.Deserialize<ApprovalsSearchModel>(json) is { } search)
        {
            HttpContext.Session.SetString(
                SessionKeys.MyTasklist,
                JsonSerializer.Serialize(new ApprovalsSearchModel { IrasId = search.IrasId })
            );
        }

        return RedirectToAction(nameof(Index));
    }
}