using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Authorize(Policy = Workspaces.SystemAdministration)]
[Route("[controller]/[action]", Name = "soc:[action]")]
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
        string? sortField = nameof(SponsorOrganisationDto.SponsorOrganisationName),
        string? sortDirection = SortDirections.Ascending,
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
            request, 1, int.MaxValue, sortField, sortDirection);

        var items = response.Content?.SponsorOrganisations ?? Enumerable.Empty<SponsorOrganisationDto>();
        var sorted = SortSponsorOrganisations(items, sortField, sortDirection, pageNumber, pageSize).ToList();
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

            if (organisationName.Length < 3)
            {
                ModelState.AddModelError("SponsorOrganisation",
                    "Please provide 3 or more characters to search sponsor organisation.");
                return View("SetupSponsorOrganisation", model);
            }

            var nameSearch = await rtsService.GetOrganisationsByName(organisationName, OrganisationRoles.Sponsor, 1, int.MaxValue);
            if (!nameSearch.IsSuccessStatusCode)
            {
                TempData[TempDataKeys.ShowNoResultsFound] = true;
                return View("SetupSponsorOrganisation", model);
            }

            var rtsOrg = nameSearch.Content?.Organisations.FirstOrDefault();
            if (rtsOrg == null ||
                !string.Equals(rtsOrg.Name?.Trim(), organisationName?.Trim(), StringComparison.Ordinal))
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

        TempData[TempDataKeys.SponsorOrganisationType] = "add";
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
        int pageNumber = 1, int pageSize = 20, string? sortField = "GivenName", string? sortDirection = "asc")
    {
        var load = await LoadSponsorOrganisationAsync(rtsId);

        var model = new SponsorOrganisationListUsersModel { SponsorOrganisation = load.Model };
        var totalUserCount = 0;

        if (load.Dto.Users?.Any() == true)
        {
            var userIds = load.Dto.Users.Select(x => x.UserId.ToString());
            var users = await userService.GetUsersByIds(userIds, searchQuery, 1, int.MaxValue);
            model.Users = users.Content?.Users.Select(u => new UserViewModel(u)) ?? [];
            totalUserCount = users.Content?.TotalCount ?? 0;
        }

        model.Users = SortSponsorOrganisationUsers(model.Users, model.SponsorOrganisation.Users, sortField,
            sortDirection, pageNumber, pageSize);

        model.Pagination = BuildPagination(pageNumber, pageSize, totalUserCount, "soc:viewsponsororganisationusers",
            sortField, sortDirection,
            new Dictionary<string, string> { { "rtsId", rtsId }, { "searchQuery", searchQuery ?? string.Empty } });
        model.Pagination.SearchQuery = searchQuery;

        return View(model);
    }

    [HttpGet]
    [Route("/sponsororganisations/viewadduser", Name = "soc:viewadduser")]
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

    [HttpPost]
    [Route("/sponsororganisations/submitadduser", Name = "soc:submitadduser")]
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
        await userService.UpdateRoles(user.Content!.User.Email, null, Roles.Sponsor);

        TempData[TempDataKeys.ShowNotificationBanner] = true;
        TempData[TempDataKeys.SponsorOrganisationUserType] = "add";
        return RedirectToAction("ViewSponsorOrganisationUsers", new { rtsId });
    }

    [HttpGet]
    [Route("/sponsororganisations/viewuser", Name = "soc:viewsponsororganisationuser")]
    [CmsContentAction(nameof(ViewSponsorOrganisationUser))]
    public async Task<IActionResult> ViewSponsorOrganisationUser(string rtsId, Guid userId, bool addUser = false)
    {
        var model = await BuildSponsorOrganisationUserModel(rtsId, userId);

        TempData[TempDataKeys.ShowEditLink] = false;

        // Auto-set type based on whether the user is being added or edited
        ViewBag.Type = addUser ? "add" : "edit";

        return View("ViewSponsorOrganisationUser", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/sponsororganisations/enableuser", Name = "soc:enableuser")]
    public async Task<IActionResult> EnableUser(string rtsId, Guid userId)
    {
        var model = await BuildSponsorOrganisationUserModel(rtsId, userId);
        return View("ConfirmEnableUser", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/sponsororganisations/disableuser", Name = "soc:disableuser")]
    public async Task<IActionResult> DisableUser(string rtsId, Guid userId)
    {
        var model = await BuildSponsorOrganisationUserModel(rtsId, userId);
        return View("ConfirmDisableUser", model);
    }

    [HttpPost]
    [Route("/sponsororganisations/confirmenableuser", Name = "soc:confirmenableuser")]
    public async Task<IActionResult> ConfirmEnableUser(string rtsId, Guid userId)
    {
        await sponsorOrganisationService.EnableUserInSponsorOrganisation(rtsId, userId);
        TempData[TempDataKeys.ShowNotificationBanner] = true;
        TempData[TempDataKeys.SponsorOrganisationUserType] = "enable";
        return RedirectToAction("ViewSponsorOrganisationUsers", new { rtsId });
    }

    [HttpPost]
    [Route("/sponsororganisations/confirmdisableuser", Name = "soc:confirmdisableuser")]
    public async Task<IActionResult> ConfirmDisableUser(string rtsId, Guid userId)
    {
        await sponsorOrganisationService.DisableUserInSponsorOrganisation(rtsId, userId);
        TempData[TempDataKeys.ShowNotificationBanner] = true;
        TempData[TempDataKeys.SponsorOrganisationUserType] = "disable";
        return RedirectToAction("ViewSponsorOrganisationUsers", new { rtsId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/sponsororganisations/enablesponsororganisation", Name = "soc:enablesponsororganisation")]
    public async Task<IActionResult> EnableSponsorOrganisation(string rtsId)
    {
        var model = await LoadSponsorOrganisationAsync(rtsId);
        return View("ConfirmEnableSponsorOrganisation", model.Model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/sponsororganisations/disablesponsororganisation", Name = "soc:disablesponsororganisation")]
    public async Task<IActionResult> DisableSponsorOrganisation(string rtsId)
    {
        var model = await LoadSponsorOrganisationAsync(rtsId);
        return View("ConfirmDisableSponsorOrganisation", model.Model);
    }

    [HttpPost]
    [Route("/sponsororganisations/confirmenablesponsororganisation", Name = "soc:confirmenablesponsororganisation")]
    public async Task<IActionResult> ConfirmEnableSponsorOrganisation(string rtsId)
    {
        await sponsorOrganisationService.EnableSponsorOrganisation(rtsId);
        TempData[TempDataKeys.SponsorOrganisationType] = "enable";
        TempData[TempDataKeys.ShowNotificationBanner] = true;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Route("/sponsororganisations/confirmdisablesponsororganisation", Name = "soc:confirmdisablesponsororganisation")]
    public async Task<IActionResult> ConfirmDisableSponsorOrganisation(string rtsId)
    {
        await sponsorOrganisationService.DisableSponsorOrganisation(rtsId);
        TempData[TempDataKeys.SponsorOrganisationType] = "disable";
        TempData[TempDataKeys.ShowNotificationBanner] = true;
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Route("/sponsororganisations/audittrail", Name = "soc:audittrail")]
    public async Task<IActionResult> AuditTrail(string rtsId, int pageNumber = 1, int pageSize = 20, string? sortField = "DateTimeStamp", string? sortDirection = "desc")
    {
        var load = await LoadSponsorOrganisationAsync(rtsId);

        var response = await sponsorOrganisationService.SponsorOrganisationAuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection);

        var auditTrailResponse = response?.Content;
        var items = auditTrailResponse?.Items;

        var sorted = SortSponsorOrganisationAuditTrails(items, sortField, sortDirection,
            load.Model.SponsorOrganisationName, pageNumber, pageSize);

        var paginationModel = new PaginationViewModel(pageNumber, pageSize,
            auditTrailResponse != null ? auditTrailResponse.TotalCount : -1)
        {
            RouteName = "soc:audittrail",
            AdditionalParameters =
            {
                { "rtsId", rtsId }
            },
            SortField = sortField,
            SortDirection = sortDirection
        };

        var resultModel = new SponsorOrganisationAuditTrailViewModel()
        {
            RtsId = rtsId,
            SponsorOrganisation = load.Model.SponsorOrganisationName,
            Pagination = paginationModel,
            Items = sorted!
        };

        return View("AuditTrail", resultModel);
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
        if (dto is null)
        {
            return (null, null, null); // not found; let caller decide redirect
        }

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
            UpdatedDate = dto.UpdatedDate ?? dto.CreatedDate,
            Users = dto.Users
        };

        return (model, dto, null);
    }

    // Sorting extracted and made stable/consistent4
    [NonAction]
    private static IEnumerable<SponsorOrganisationDto> SortSponsorOrganisations(
        IEnumerable<SponsorOrganisationDto> items,
        string? sortField,
        string? sortDirection,
        int pageNumber = 1,
        int pageSize = 20)
    {
        static string CountriesKey(SponsorOrganisationDto x)
        {
            return x.Countries == null || !x.Countries.Any()
                ? string.Empty
                : string.Join(", ", x.Countries.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
        }

        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        var sorted = sortField?.ToLowerInvariant() switch
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

        // Apply pagination
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        var skip = (pageNumber - 1) * pageSize;

        return sorted.Skip(skip).Take(pageSize);
    }

    [NonAction]
    private static IEnumerable<UserViewModel> SortSponsorOrganisationUsers(
        IEnumerable<UserViewModel> users,
        IEnumerable<SponsorOrganisationUserDto>? sponsorOrganisationUserDtos,
        string? sortField,
        string? sortDirection,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var list = users as IList<UserViewModel> ?? users.ToList();

        // Lookup: UserId -> IsActive (safe if null)
        var statusByUserId = sponsorOrganisationUserDtos?
                                 .GroupBy(x => x.UserId)
                                 .ToDictionary(g => g.Key, g => g.Last().IsActive)
                             ?? new Dictionary<Guid, bool>();

        // Update Status on the models (optional but requested)
        foreach (var u in list)
        {
            if (Guid.TryParse(u.Id?.Trim(), out var gid) && statusByUserId.TryGetValue(gid, out var active))
            {
                u.Status = active ? "Active" : "Disabled";
            }
        }

        // Helpers
        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortField?.ToLowerInvariant() ?? string.Empty;

        // Primary: status ordering
        var ordered =
            field == "status"
                ? desc ? list.OrderByDescending(IsActive) : list.OrderBy(IsActive)
                : list.OrderByDescending(IsActive); // default bubble Active first

        // Secondary: selected field (text fields share the same path)
        if (field == "currentlogin")
        {
            ordered = desc ? ordered.ThenByDescending(LatestLogin) : ordered.ThenBy(LatestLogin);
        }
        else if (field != "status")
        {
            Func<UserViewModel, string> key = field switch
            {
                "familyname" => x => x.FamilyName ?? string.Empty,
                "email" => x => x.Email ?? string.Empty,
                _ => x => x.GivenName ?? string.Empty // default
            };

            ordered = desc
                ? ordered.ThenByDescending(key, StringComparer.OrdinalIgnoreCase)
                : ordered.ThenBy(key, StringComparer.OrdinalIgnoreCase);
        }

        // Tertiary tie-break (unless already sorting by currentlogin)
        if (field != "currentlogin")
        {
            ordered = ordered.ThenByDescending(LatestLogin);
        }

        // Pagination
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, pageSize);

        var skip = (pageNumber - 1) * pageSize;
        return ordered.Skip(skip).Take(pageSize);

        static DateTime LatestLogin(UserViewModel x)
        {
            return ((x.CurrentLogin ?? DateTime.MinValue) > (x.LastLogin ?? DateTime.MinValue)
                ? x.CurrentLogin
                : x.LastLogin) ?? DateTime.MinValue;
        }

        bool IsActive(UserViewModel u)
        {
            return Guid.TryParse(u.Id?.Trim(), out var g) && statusByUserId.TryGetValue(g, out var a) && a;
        }
    }

    [NonAction]
    private static IEnumerable<SponsorOrganisationAuditTrailDto> SortSponsorOrganisationAuditTrails(
    IEnumerable<SponsorOrganisationAuditTrailDto> items,
    string? sortField,
    string? sortDirection,
    string? sponsorOrganisationName,
    int pageNumber = 1,
    int pageSize = 20)
    {
        // 1) Transform descriptions: replace RtsId with sponsorOrganisationName (case-insensitive)
        var list = items
            .Select(x =>
            {
                var desc = x.Description ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(sponsorOrganisationName) && !string.IsNullOrWhiteSpace(x.RtsId))
                {
                    // Replace all occurrences of RtsId with sponsorOrganisationName (case-insensitive, no regex)
                    desc = desc.Replace(x.RtsId, sponsorOrganisationName!, StringComparison.OrdinalIgnoreCase);
                }

                return new SponsorOrganisationAuditTrailDto
                {
                    Id = x.Id,
                    RtsId = x.RtsId,
                    DateTimeStamp = x.DateTimeStamp,
                    Description = desc,
                    User = x.User
                };
            })
            .ToList();

        // 2) Sorting
        var descSort = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortField?.ToLowerInvariant();

        var sorted = field switch
        {
            "description" => descSort
                ? list.OrderByDescending(x => x.Description, StringComparer.OrdinalIgnoreCase)
                      .ThenByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.Description, StringComparer.OrdinalIgnoreCase)
                      .ThenByDescending(x => x.DateTimeStamp),

            "user" => descSort
                ? list.OrderByDescending(x => x.User, StringComparer.OrdinalIgnoreCase)
                      .ThenByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.User, StringComparer.OrdinalIgnoreCase)
                      .ThenByDescending(x => x.DateTimeStamp),

            // Allow common aliases for the timestamp
            "datetimestamp" => descSort
                ? list.OrderByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.DateTimeStamp),

            // Default = most recent first (typical for audit trails)
            _ => list.OrderByDescending(x => x.DateTimeStamp)
        };

        // 3) Pagination safety + apply
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var skip = (pageNumber - 1) * pageSize;
        return sorted.Skip(skip).Take(pageSize);
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

    [NonAction]
    private async Task<SponsorOrganisationUserModel> BuildSponsorOrganisationUserModel(string rtsId, Guid userId)
    {
        var sponsorOrganisation = await LoadSponsorOrganisationAsync(rtsId);
        var userResponse = await userService.GetUser(userId.ToString(), null);
        var sponsorOrganisationUser = await sponsorOrganisationService.GetUserInSponsorOrganisation(rtsId, userId);

        return new SponsorOrganisationUserModel
        {
            SponsorOrganisation = sponsorOrganisation.Model,
            User = userResponse.Content is not null
                ? new UserViewModel(userResponse.Content)
                : new UserViewModel(),
            SponsorOrganisationUser = sponsorOrganisationUser.Content ?? new SponsorOrganisationUserDto()
        };
    }
}