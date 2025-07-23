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

[Route("[controller]/[action]", Name = "tasklist:[action]")]
[Authorize(Policy = "IsUser")]
public class ModificationsTasklistController(IApplicationsService applicationsService, IValidator<ApprovalsSearchModel> validator) : Controller
{
    private const string ModificationToAssignNotSelectedErrorMessage = "You have not selected a modification to assign. Select at least one modification before you can continue.";

    [HttpGet]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        List<string>? selectedModificationIds = null,
        string? sortField = nameof(ModificationsModel.CreatedAt),
        string? sortDirection = SortDirections.Ascending)
    {
        var model = new ModificationsTasklistViewModel
        {
            SelectedModificationIds = selectedModificationIds ?? [],
            EmptySearchPerformed = true // Set to true to check if search bar should be hidden on view
        };

        if (TempData.Peek(TempDataKeys.ApprovalsSearchModel) is string json)
        {
            model.Search = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
            if (model.Search.Filters.Count != 0 || !string.IsNullOrEmpty(model.Search.IrasId))
            {
                model.EmptySearchPerformed = false;
            }
        }

        var searchQuery = new ModificationSearchRequest()
        {
            Country = ["England"],
            ShortProjectTitle = model.Search.ShortProjectTitle,
            FromDate = model.Search.FromDate,
            ToDate = model.Search.ToDate,
            IrasId = model.Search.IrasId,
        };

        var querySortField = sortField;
        var querySortDirection = sortDirection;

        if (sortField == nameof(ModificationsModel.DaysSinceSubmission))
        {
            querySortField = nameof(ModificationsModel.CreatedAt);
            querySortDirection = sortDirection == SortDirections.Ascending
                ? SortDirections.Descending
                : SortDirections.Ascending;
        }

        var result = await applicationsService.GetModifications(searchQuery, pageNumber, pageSize, querySortField, querySortDirection);
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
                }
            })
            .ToList() ?? [];

        // mark selected modifications as such in the view model
        foreach (var mod in model.Modifications)
        {
            if (selectedModificationIds?.Contains(mod.Modification.ModificationId) == true)
            {
                mod.IsSelected = true;
            }
        }

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, result?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "tasklist-selection"
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignModifications(List<string> selectedModificationIds)
    {
        if (selectedModificationIds == null || !selectedModificationIds.Any())
        {
            ModelState.AddModelError(ModificationsTasklist.ModificationToAssignNotSelected, ModificationToAssignNotSelectedErrorMessage);
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }
        else
        {
            // logic for assigning modifications
            throw new NotImplementedException();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ApplyFilters(ModificationsTasklistViewModel model)
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

        TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(model.Search);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key)
    {
        if (!TempData.TryGetValue(TempDataKeys.ApprovalsSearchModel, out var tempDataValue))
        {
            return RedirectToAction(nameof(Index));
        }

        var json = tempDataValue?.ToString();
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

            case "datemodificationsubmitted-from":
                search.FromDay = search.FromMonth = search.FromYear = null;
                break;

            case "datemodificationsubmitted-to":
                search.ToDay = search.ToMonth = search.ToYear = null;
                break;
        }

        TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(search);

        return await ApplyFilters(new ModificationsTasklistViewModel { Search = search });
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        if (!TempData.TryGetValue(TempDataKeys.ApprovalsSearchModel, out var tempDataValue))
        {
            return RedirectToAction(nameof(Index));
        }

        var json = tempDataValue?.ToString();
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

        TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(cleanedSearch);

        return RedirectToAction(nameof(Index));
    }
}