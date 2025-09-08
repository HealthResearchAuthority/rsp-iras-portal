using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "tasklist:[action]")]
[Authorize(Roles = "system_administrator,workflow_co-ordinator,team_manager,study-wide_reviewer")]
public class ModificationsTasklistController(
    IProjectModificationsService projectModificationsService,
    IUserManagementService userManagementService,
    IValidator<ApprovalsSearchModel> validator) : Controller
{
    private const string ModificationToAssignNotSelectedErrorMessage = "Select at least one modification";

    [HttpGet]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        List<string>? selectedModificationIds = null,
        string sortField = nameof(ModificationsModel.CreatedAt),
        string sortDirection = SortDirections.Ascending)
    {
        var model = new ModificationsTasklistViewModel
        {
            SelectedModificationIds = selectedModificationIds ?? [],
            EmptySearchPerformed = true // Set to true to check if search bar should be hidden on view
        };

        var json = HttpContext.Session.GetString(SessionKeys.ModificationsTasklist);
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
            ReviewerId = null
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

        var result = await projectModificationsService.GetModifications(
            searchQuery, pageNumber, pageSize, querySortField, querySortDirection);

        model.Modifications = result?.Content?.Modifications?
            .Select(dto => new TaskListModificationViewModel
            {
                Modification = new ModificationsModel
                {
                    Id = dto.Id,
                    ProjectRecordId = dto.ProjectRecordId,
                    ModificationId = dto.ModificationId,
                    ShortProjectTitle = dto.ShortProjectTitle,
                    ModificationType = dto.ModificationType,
                    ChiefInvestigator = dto.ChiefInvestigator,
                    LeadNation = dto.LeadNation,
                    SponsorOrganisation = dto.SponsorOrganisation,
                    CreatedAt = dto.CreatedAt
                },
                IsSelected = selectedModificationIds?.Contains(dto.Id) ?? false,
            })
            .ToList() ?? [];

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
        if (selectedModificationIds == null || selectedModificationIds.Count == 0)
        {
            ModelState.AddModelError(ModificationsTasklist.ModificationToAssignNotSelected, ModificationToAssignNotSelectedErrorMessage);
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        var getModificationsResponse = await projectModificationsService.GetModificationsByIds(selectedModificationIds);

        if (!getModificationsResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "There was a problem retrieving the selected modifications");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        var modifications = getModificationsResponse.Content?.Modifications?
            .Select(dto => new ModificationsModel
            {
                Id = dto.Id,
                ModificationId = dto.ModificationId,
                ShortProjectTitle = dto.ShortProjectTitle,
            })
            .ToList() ?? [];

        var getUsersResponse = await userManagementService.GetUsers(new SearchUserRequest
        {
            Role = ["043aca8e-f88e-473e-974c-262f846285ea"] // Search for users with Study-wide reviewer role
        });

        if (!getUsersResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "There was a problem retrieving the list of reviewers");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        var reviewers = getUsersResponse.Content?.Users;

        return View((modifications, reviewers));
    }

    [HttpPost]
    public async Task<IActionResult> AssignModifications(List<string> modificationIds, string reviewerId)
    {
        if (modificationIds == null || modificationIds.Count == 0 || string.IsNullOrEmpty(reviewerId))
        {
            ModelState.AddModelError(string.Empty, "There was a problem assigning the modifications");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        var serviceResponse = await projectModificationsService.AssignModificationsToReviewer(modificationIds, reviewerId);

        if (!serviceResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "There was a problem assigning the modifications");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        TempData.TryAdd(TempDataKeys.ModificationTasklistReviewerId, reviewerId);

        return RedirectToAction(nameof(AssignmentSuccess));
    }

    [HttpGet]
    public async Task<IActionResult> AssignmentSuccess()
    {
        TempData.TryGetValue(TempDataKeys.ModificationTasklistReviewerId, out var reviewerId);

        var getUserResponse = await userManagementService.GetUser(reviewerId?.ToString(), null);

        var reviewer = getUserResponse.Content?.User;

        return View(reviewer);
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

        HttpContext.Session.SetString(SessionKeys.ModificationsTasklist, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key)
    {
        var json = HttpContext.Session.GetString(SessionKeys.ModificationsTasklist);
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

        HttpContext.Session.SetString(SessionKeys.ModificationsTasklist, JsonSerializer.Serialize(search));

        return await ApplyFilters(new ModificationsTasklistViewModel { Search = search });
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        var json = HttpContext.Session.GetString(SessionKeys.ModificationsTasklist);
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

        HttpContext.Session.SetString(SessionKeys.ModificationsTasklist,
            JsonSerializer.Serialize(cleanedSearch));

        return RedirectToAction(nameof(Index));
    }
}