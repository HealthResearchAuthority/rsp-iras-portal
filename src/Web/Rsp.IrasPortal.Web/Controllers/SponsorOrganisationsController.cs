using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "soc:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
public class SponsorOrganisationsController(
    ISponsorOrganisationService sponsorOrganisationService,
    IRtsService rtsService,
    IUserManagementService userService
) : Controller
{
    /// <summary>
    ///     Displays a list of sponsor organisations
    /// </summary>
    [HttpGet]
    [HttpPost]
    [Route("/sponsororganisations", Name = "soc:viewsponsororganisations")]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = "name",
        string? sortDirection = "asc",
        [FromForm] SponsorOrganisationSearchViewModel? model = null,
        [FromQuery] bool fromPagination = false)
    {
        if (!fromPagination)
        {
            pageNumber = 1;
            pageSize = 20;
        }

        // Restore any saved search (for GET), merge with incoming
        model = RestoreSearchFromSession(model, HttpContext.Request.Method == HttpMethods.Get);

        var request = new SponsorOrganisationSearchRequest
        {
            SearchQuery = model.Search.SearchQuery,
            Country = model.Search.Country,
            Status = model.Search.Status
        };

        var response = await sponsorOrganisationService.GetAllSponsorOrganisations(
            request, pageNumber, pageSize, sortField, sortDirection);

        var items = response.Content?.SponsorOrganisations ?? Enumerable.Empty<SponsorOrganisationDto>();
        var sorted = SortSponsorOrganisations(items, sortField, sortDirection).ToList();
        var total = response.Content?.TotalCount ?? 0;

        var viewModel = new SponsorOrganisationSearchViewModel
        {
            SponsorOrganisations = sorted,
            Search = model.Search,
            Pagination = BuildPagination(pageNumber, pageSize, total, "soc:viewsponsororganisations", sortField,
                sortDirection)
        };

        // Persist filters if any are applied
        SaveFiltersToSessionIfAny(model);

        return View("ViewSponsorOrganisations", viewModel);
    }

    [Route("/sponsororganisations/applyfilters", Name = "soc:applyfilters")]
    [HttpPost]
    [HttpGet]
    [CmsContentAction(nameof(Index))]
    public Task<IActionResult> ApplyFilters(
        SponsorOrganisationSearchViewModel model,
        string? sortField = "name",
        string? sortDirection = "asc",
        [FromQuery] bool fromPagination = false)
    {
        HttpContext.Session.SetString(
            SessionKeys.SponsorOrganisationsSearch,
            JsonSerializer.Serialize(model.Search ?? new SponsorOrganisationSearchModel()));

        // PRG: redirect to Index with query params (no model in body)
        IActionResult result = RedirectToAction(nameof(Index), new
        {
            pageNumber = 1,
            pageSize = 20,
            sortField,
            sortDirection,
            fromPagination
        });

        return Task.FromResult(result);
    }

    /// <summary>Displays the empty review body to create</summary>
    [HttpGet]
    [Route("/sponsororganisations/setup", Name = "soc:setupsponsororganisation")]
    public IActionResult SetupSponsorOrganisation()
    {
        return View("SetupSponsorOrganisation", new SponsorOrganisationSetupViewModel());
    }

    /// <summary>Check sponsor organisation details</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/sponsororganisations/check", Name = "soc:checksponsororganisation")]
    [CmsContentAction(nameof(SetupSponsorOrganisation))]
    public async Task<IActionResult> CheckSponsorOrganisation(SponsorOrganisationSetupViewModel model)
    {
        ModelState.Clear();
        TempData[TempDataKeys.ShowNoResultsFound] = null;

        if (model.SponsorOrganisation != null || model.SponsorOrgSearch.SelectedOrganisation != null)
        {
            var organisationName = model.SponsorOrganisation ?? model.SponsorOrgSearch.SelectedOrganisation;

            var nameSearch = await rtsService.GetOrganisationsByName(organisationName, null, 1, int.MaxValue);
            if (!nameSearch.IsSuccessStatusCode)
            {
                return this.ServiceError(nameSearch);
            }

            var rtsOrg = nameSearch.Content?.Organisations.FirstOrDefault();
            if (rtsOrg == null)
            {
                TempData[TempDataKeys.ShowNoResultsFound] = true;
                return View("SetupSponsorOrganisation", model);
            }

            var existing = await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsOrg.Id);
            if (!existing.IsSuccessStatusCode)
            {
                return this.ServiceError(existing);
            }

            if (existing.Content.TotalCount > 0)
            {
                ModelState.AddModelError("SponsorOrganisation", "A sponsor organisation with this name already exists");
                return View("SetupSponsorOrganisation", model);
            }

            return RedirectToAction("ConfirmSponsorOrganisation", new SponsorOrganisationModel
            {
                SponsorOrganisationName = rtsOrg.Name,
                Countries = [rtsOrg.CountryName],
                RtsId = rtsOrg.Id
            });
        }

        return View("SetupSponsorOrganisation", model);
    }

    /// <summary>Displays the confirmation page</summary>
    [HttpGet]
    [Route("/sponsororganisations/confirm", Name = "soc:sponsororganisation")]
    public IActionResult ConfirmSponsorOrganisation(SponsorOrganisationModel model)
    {
        return View("ConfirmSponsorOrganisation", model);
    }

    /// <summary>Save sponsor organisation</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/sponsororganisations/save", Name = "soc:savesponsororganisation")]
    public async Task<IActionResult> SaveSponsorOrganisation(SponsorOrganisationModel model)
    {
        model.CreatedBy = User?.Identity?.Name!;
        model.CreatedDate = DateTime.UtcNow;

        var dto = model.Adapt<SponsorOrganisationDto>();
        var response = await sponsorOrganisationService.CreateSponsorOrganisation(dto);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        TempData[TempDataKeys.ShowNotificationBanner] = true;
        return RedirectToAction("Index");
    }

    /// <summary>Search RTS organisations</summary>
    public async Task<IActionResult> SearchOrganisations(SponsorOrganisationSetupViewModel model, string? role,
        int? pageSize = 5, int pageIndex = 1)
    {
        var returnUrl = TempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        TempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");
        model.SponsorOrgSearch.SelectedOrganisation = string.Empty;
        TempData.TryAdd(TempDataKeys.OrgSearch, model.SponsorOrgSearch, true);

        if (string.IsNullOrWhiteSpace(model.SponsorOrgSearch.SearchText) ||
            model.SponsorOrgSearch.SearchText.Length < 3)
        {
            ModelState.AddModelError("sponsor_org_search",
                "Please provide 3 or more characters to search sponsor organisation.");
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return Redirect(returnUrl!);
        }

        role ??= OrganisationRoles.Sponsor;

        var searchResponse =
            await rtsService.GetOrganisationsByName(model.SponsorOrgSearch.SearchText, role, pageIndex, pageSize);
        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return this.ServiceError(searchResponse);
        }

        TempData.TryAdd(TempDataKeys.SponsorOrganisations, searchResponse.Content, true);
        return Redirect(returnUrl!);
    }

    /// <summary>Displays a single sponsor organisation</summary>
    [HttpGet]
    [Route("/sponsororganisations/view", Name = "soc:viewsponsororganisation")]
    public async Task<IActionResult> ViewSponsorOrganisation(string rtsId)
    {
        var load = await LoadSponsorOrganisationAsync(rtsId);
        if (load.ErrorResult is not null)
        {
            return load.ErrorResult;
        }

        return View(load.Model);
    }

    /// <summary>Displays users for a sponsor organisation</summary>
    [HttpGet]
    [Route("/sponsororganisations/viewusers", Name = "soc:viewsponsororganisationusers")]
    public async Task<IActionResult> ViewSponsorOrganisationUsers(string rtsId, string? searchQuery = null,
        int pageNumber = 1, int pageSize = 20)
    {
        var load = await LoadSponsorOrganisationAsync(rtsId);

        var model = new SponsorOrganisationListUsersModel { SponsorOrganisation = load.Model };
        var totalUserCount = 0;

        if (load.Dto.Users?.Any() == true)
        {
            var userIds = load.Dto.Users.Select(x => x.UserId.ToString());
            var users = await userService.GetUsersByIds(userIds, searchQuery, pageNumber, pageSize);
            model.Users = users.Content?.Users.Select(u => new UserViewModel(u)) ?? [];
            totalUserCount = users.Content?.TotalCount ?? 0;
        }

        model.Pagination = BuildPagination(pageNumber, pageSize, totalUserCount, "soc:viewsponsororganisationusers",
            null, null,
            new Dictionary<string, string> { { "rtsId", rtsId }, { "searchQuery", searchQuery ?? string.Empty } });
        model.Pagination.SearchQuery = searchQuery;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ViewAddUser(string rtsId, string? searchQuery = null, int pageNumber = 1,
        int pageSize = 20)
    {
        var load = await LoadSponsorOrganisationAsync(rtsId);

        var model = new SponsorOrganisationListUsersModel { SponsorOrganisation = load.Model };

        // Only search when user typed something
        if (!string.IsNullOrWhiteSpace(searchQuery) && load.Dto.Users is not null)
        {
            var existingIds = load.Dto.Users.Select(x => x.UserId.ToString()).ToArray();
            var users = await userService.SearchUsers(searchQuery, existingIds, pageNumber, pageSize);

            model.Users = users.Content?.Users.Select(u => new UserViewModel(u)) ?? [];
            model.Pagination = BuildPagination(
                pageNumber, pageSize, users.Content?.TotalCount ?? 0, "soc:viewadduser", null, null,
                new Dictionary<string, string> { { "rtsId", rtsId } });
            model.Pagination.SearchQuery = searchQuery;
        }
        else
        {
            // still provide pagination baseline for the view
            model.Pagination = BuildPagination(pageNumber, pageSize, 0, "soc:viewadduser", null, null,
                new Dictionary<string, string> { { "rtsId", rtsId } });
            model.Pagination.SearchQuery = searchQuery;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmAddUpdateUser(string rtsId, Guid userId)
    {
        var load = await LoadSponsorOrganisationAsync(rtsId);

        var user = await userService.GetUser(userId.ToString(), null);

        var model = new ConfirmAddUpdateSponsorOrganisationUserModel
        {
            SponsorOrganisation = load.Model,
            User = user.Content is not null ? new UserViewModel(user.Content) : new UserViewModel()
        };

        TempData[TempDataKeys.ShowEditLink] = false;
        return View("ConfirmAddUpdateUser", model);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitAddUser(string rtsId, Guid userId, Guid sponsorOrganisationId)
    {
        var user = await userService.GetUser(userId.ToString(), null);

        var dto = new SponsorOrganisationUserDto
        {
            Id = sponsorOrganisationId,
            RtsId = rtsId,
            UserId = userId,
            Email = user.Content?.User.Email,
            DateAdded = DateTime.UtcNow
        };

        var response = await sponsorOrganisationService.AddUserToSponsorOrganisation(dto);
        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        // Assign sponsor role on success
        await userService.UpdateRoles(user.Content!.User.Email, null, "sponsor");

        TempData[TempDataKeys.ShowNotificationBanner] = true;
        return RedirectToAction("ViewSponsorOrganisationUsers", new { rtsId });
    }

    // ---------------------------
    // Private helpers (de-duplication)
    // ---------------------------

    // Centralised RTS + Portal fetch + mapping
    [NonAction]
    private async Task<(SponsorOrganisationModel? Model, SponsorOrganisationDto? Dto, IActionResult? ErrorResult)>
        LoadSponsorOrganisationAsync(string rtsId)
    {
        var rbResponse = await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsId);
        if (!rbResponse.IsSuccessStatusCode)
        {
            return (null, null, this.ServiceError(rbResponse));
        }

        var dto = rbResponse.Content?.SponsorOrganisations?.FirstOrDefault();

        var orgResponse = await rtsService.GetOrganisation(rtsId);
        if (!orgResponse.IsSuccessStatusCode)
        {
            return (null, null, this.ServiceError(orgResponse));
        }

        var model = new SponsorOrganisationModel
        {
            Id = dto.Id,
            RtsId = rtsId,
            SponsorOrganisationName = orgResponse.Content!.Name,
            Countries = [orgResponse.Content!.CountryName],
            IsActive = dto.IsActive,
            UpdatedDate = dto.UpdatedDate ?? dto.CreatedDate
        };

        return (model, dto, null);
    }

    // Sorting extracted and made stable/consistent
    [NonAction]
    private static IEnumerable<SponsorOrganisationDto> SortSponsorOrganisations(
        IEnumerable<SponsorOrganisationDto> items, string? sortField, string? sortDirection)
    {
        static string CountriesKey(SponsorOrganisationDto x)
        {
            return x.Countries == null || !x.Countries.Any()
                ? string.Empty
                : string.Join(", ", x.Countries.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
        }

        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortField?.ToLowerInvariant() switch
        {
            "sponsororganisationname" => desc
                ? items.OrderByDescending(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase),

            "countries" => desc
                ? items.OrderByDescending(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase),

            "isactive" => desc
                ? items.OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.IsActive)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase),

            _ => desc
                ? items.OrderByDescending(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
        };
    }

    // Pagination builder with optional extras
    [NonAction]
    private static PaginationViewModel BuildPagination(
        int pageNumber,
        int pageSize,
        int totalCount,
        string routeName,
        string? sortField,
        string? sortDirection,
        IDictionary<string, string>? extra = null)
    {
        var p = new PaginationViewModel(pageNumber, pageSize, totalCount)
        {
            RouteName = routeName,
            SortField = sortField,
            SortDirection = sortDirection
        };

        if (extra is not null)
        {
            foreach (var kv in extra)
            {
                p.AdditionalParameters[kv.Key] = kv.Value;
            }
        }

        return p;
    }

    // Session restore + merge
    [NonAction]
    [ExcludeFromCodeCoverage]
    private SponsorOrganisationSearchViewModel RestoreSearchFromSession(
        SponsorOrganisationSearchViewModel? incoming, bool restoreFromSession)
    {
        var model = incoming ?? new SponsorOrganisationSearchViewModel();

        if (!restoreFromSession)
        {
            return model;
        }

        var saved = HttpContext.Session.GetString(SessionKeys.SponsorOrganisationsSearch);
        if (string.IsNullOrWhiteSpace(saved))
        {
            return model;
        }

        var savedSearch = JsonSerializer.Deserialize<SponsorOrganisationSearchModel>(saved);
        if (savedSearch is null)
        {
            return model;
        }

        // Merge: incoming wins if user posted values; otherwise fallback to saved
        model.Search = new SponsorOrganisationSearchModel();
        model.Search.SearchQuery = string.IsNullOrWhiteSpace(model.Search.SearchQuery)
            ? savedSearch.SearchQuery
            : model.Search.SearchQuery;
        model.Search.Status = model.Search.Status ?? savedSearch.Status;
        model.Search.Country = model.Search.Country is { Count: > 0 }
            ? model.Search.Country
            : savedSearch.Country ?? [];

        return model;
    }

    // Persist only when filters are really applied
    [NonAction]
    [ExcludeFromCodeCoverage]
    private void SaveFiltersToSessionIfAny(SponsorOrganisationSearchViewModel model)
    {
        var hasFilters =
            !string.IsNullOrWhiteSpace(model.Search.SearchQuery) ||
            (model.Search.Country?.Count ?? 0) > 0 ||
            model.Search.Status.HasValue;

        if (!hasFilters)
        {
            return;
        }

        HttpContext.Session.SetString(
            SessionKeys.SponsorOrganisationsSearch,
            JsonSerializer.Serialize(model.Search));
    }
}