using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Filters;
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
    IReviewBodyService reviewBodyService,
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
        const string SessionSelectedKey = "Tasklist:SelectedModificationIds";

        // 1) If query carries selected IDs (from back link), persist + clean URL
        if (selectedModificationIds is { Count: > 0 })
        {
            // Normalize (also split any accidental CSV values)
            var normalized = selectedModificationIds
                .SelectMany(v => (v ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            HttpContext.Session.SetString(SessionSelectedKey, JsonSerializer.Serialize(normalized));

            // Redirect WITHOUT selectedModificationIds, keeping sort/paging (and anything else you want)
            return RedirectToRoute(
                routeName: "tasklist:index",
                routeValues: new
                {
                    pageNumber,
                    pageSize,
                    sortField,
                    sortDirection
                });
        }

        // 2) Pull persisted selections from Session (if any) for rendering
        var persistedSelections = HttpContext.Session.GetString(SessionSelectedKey);
        var selectedFromSession = !string.IsNullOrEmpty(persistedSelections)
            ? JsonSerializer.Deserialize<List<string>>(persistedSelections) ?? []
            : [];

        var leadNation = await GetRelevantCountriesForUser();

        var model = new ModificationsTasklistViewModel
        {
            SelectedModificationIds = selectedFromSession,
            EmptySearchPerformed = true, // Set to true to check if search bar should be hidden on view
            LeadNation = leadNation != null ? string.Join(", ", leadNation) : string.Empty
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
            LeadNation = leadNation!,
            ShortProjectTitle = model.Search.ShortProjectTitle,
            FromDate = model.Search.FromDate,
            ToDate = model.Search.ToDate,
            IrasId = model.Search.IrasId,
            ReviewerId = null,
            IncludeReviewerId = !User.IsInRole("team_manager"),
            ReviewerName = model.Search.ReviewerName,
            IncludeReviewerName = !string.IsNullOrWhiteSpace(model.Search.ReviewerName),
        };

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
                    SponsorOrganisation = dto.SponsorOrganisation,
                    SentToSponsorDate = dto.SentToSponsorDate,
                    SentToRegulatorDate = dto.SentToRegulatorDate,
                    ChiefInvestigator = dto.ChiefInvestigator,
                    CreatedAt = dto.CreatedAt,
                    Status = dto.Status is ModificationStatus.WithReviewBody ? "Received" : dto.Status,
                    ReviewerName = dto.ReviewerName
                },
                IsSelected = selectedFromSession.Contains(dto.Id, StringComparer.OrdinalIgnoreCase),
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
                ProjectRecordId = dto.ProjectRecordId
            })
            .ToList() ?? [];

        var getUsersResponse = await userManagementService.GetUsers
        (
            new SearchUserRequest
            {
                Role = ["043aca8e-f88e-473e-974c-262f846285ea"], // Study-wide reviewer role ID
                Status = true
            },
            pageNumber: 1,
            pageSize: int.MaxValue
        );

        if (!getUsersResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "There was a problem retrieving the list of reviewers");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        var leadNation = await GetRelevantCountriesForUser();

        var getReviewBodiesResponse = await reviewBodyService.GetAllReviewBodies
        (
            new ReviewBodySearchRequest
            {
                Country = leadNation,
                Status = true
            },
            pageSize: int.MaxValue
        );

        if (!getReviewBodiesResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "There was a problem retrieving the list of reviewers");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        List<Guid> englishReviewBodyIds = getReviewBodiesResponse
            .Content?
            .ReviewBodies?
            .Select(rb => rb.Id).ToList() ?? [];

        var reviewBodyUsersResponse = await reviewBodyService.GetUserReviewBodiesByReviewBodyIds(englishReviewBodyIds);

        if (!reviewBodyUsersResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "There was a problem retrieving the list of reviewers");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        var reviewBodyUserIds = reviewBodyUsersResponse.Content?
            .Select(rbu => rbu.UserId.ToString().ToLowerInvariant())
            .Distinct()
            .ToList() ?? [];

        var reviewers = getUsersResponse.Content?.Users?.Where(user => reviewBodyUserIds.Contains(user.Id!.ToLowerInvariant()));

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

        var reviewer = await userManagementService.GetUser(reviewerId, null);

        if (!reviewer.IsSuccessStatusCode || reviewer.Content?.User == null)
        {
            ModelState.AddModelError(string.Empty, "There was a problem assigning the modifications");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Index));
        }

        var serviceResponse = await projectModificationsService
            .AssignModificationsToReviewer(modificationIds, reviewerId, reviewer.Content.User.Email, $"{reviewer.Content.User.GivenName} {reviewer.Content.User.FamilyName}");

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
    [CmsContentAction(nameof(Index))]
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

            case "study-widereviewer":
                search.ReviewerName = null;
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

    private async Task<List<string>?> GetRelevantCountriesForUser()
    {
        var leadNation = new List<string> { UkCountryNames.England };

        if (!Guid.TryParse(User?.FindFirstValue("userId"), out var userId))
        {
            // userId does not exist so exit block
            return leadNation;
        }

        if (User.IsInRole("system_administrator"))
        {
            // user is admin so they can see modifications for all contries
            leadNation = UkCountryNames.Countries;
        }
        else if (User.IsInRole("team_manager"))
        {
            // if user is team manager, then take their assigned country into account
            var userEntity = await userManagementService.GetUser(userId.ToString(), null);
            leadNation = userEntity?.Content?.User?.Country != null ?
                userEntity?.Content?.User?.Country?.Split(',')?.ToList() :
                leadNation;
        }
        else
        {
            // if user is not team manager, then take their assigned review body into account if applicable
            var bodiesResp = await reviewBodyService.GetUserReviewBodies(userId);

            var reviewBodyId = bodiesResp.IsSuccessStatusCode
                ? bodiesResp.Content?.FirstOrDefault()?.Id
                : null;

            if (reviewBodyId is { } rbId)
            {
                var rbResp = await reviewBodyService.GetReviewBodyById(rbId);
                leadNation = rbResp.Content?.Countries != null ?
                    rbResp.Content?.Countries :
                    leadNation;
            }
        }

        return leadNation;
    }
}