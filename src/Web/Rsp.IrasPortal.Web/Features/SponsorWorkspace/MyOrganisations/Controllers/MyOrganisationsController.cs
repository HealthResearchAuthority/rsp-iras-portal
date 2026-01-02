using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Web.Extensions.PaginationViewModelExtensions;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Controllers;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class MyOrganisationsController(
    ISponsorOrganisationService sponsorOrganisationService,
    IRtsService rtsService,
    IUserManagementService userService
) : Controller
{
    [Authorize(Policy = Permissions.Sponsor.MyOrganisations_Search)]
    [HttpGet]
    public async Task<IActionResult> MyOrganisations
    (
        string sortField = nameof(SponsorOrganisationDto.SponsorOrganisationName),
        string sortDirection = SortDirections.Ascending
    )
    {
        var model = new SponsorMyOrganisationsViewModel();
        var userId = User?.FindFirst(CustomClaimTypes.UserId)?.Value;

        var json = HttpContext.Session.GetString(SessionKeys.SponsorMyOrganisationsSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<SponsorMyOrganisationsSearchModel>(json)!;
        }

        var request = new SponsorOrganisationSearchRequest
        {
            SearchQuery = model.Search.SearchTerm,
            UserId = Guid.Parse(userId!),
        };

        var response = await sponsorOrganisationService.GetAllSponsorOrganisations(
            request, 1, int.MaxValue, sortField, sortDirection);

        var items = response.Content?.SponsorOrganisations ?? Enumerable.Empty<SponsorOrganisationDto>();

        var filteredOrganisations = items
            .Where(org =>
                org.Users == null ||
                !org.Users.Any(u =>
                    u.UserId == Guid.Parse(userId) &&
                    !u.IsActive))
            .ToList();

        model.MyOrganisations = filteredOrganisations.SortSponsorOrganisations(sortField, sortDirection).ToList();
        model.Pagination = new PaginationViewModel(1, int.MaxValue, 0)
        {
            SortDirection = sortDirection,
            SortField = sortField
        };

        return View(model);
    }

    [Route("/sponsorworkspace/searchmyorganisations", Name = "sws:searchmyorganisations")]
    [HttpPost]
    [CmsContentAction(nameof(Index))]
    public Task<IActionResult> SearchMyOrganisations(
        SponsorMyOrganisationsViewModel model,
        string? sortField = "SponsorOrganisationName",
        string? sortDirection = "asc")
    {
        HttpContext.Session.SetString(
            SessionKeys.SponsorMyOrganisationsSearch,
            JsonSerializer.Serialize(model.Search ?? new SponsorMyOrganisationsSearchModel()));

        // PRG: redirect to Index with query params (no model in body)
        IActionResult result = RedirectToAction(nameof(MyOrganisations), new
        {
            sortField,
            sortDirection
        });

        return Task.FromResult(result);
    }

    [Authorize(Policy = Permissions.Sponsor.MyOrganisations_Profile)]
    [HttpGet]
    public async Task<IActionResult> MyOrganisationProfile(string rtsId)
    {
        ViewBag.Active = MyOrganisationProfileOverview.Profile;

        var ctxResult = await TryGetSponsorOrgContext(rtsId);
        if (ctxResult.HasResult)
        {
            return ctxResult.Result!;
        }

        var ctx = ctxResult.Context!;

        var model = new SponsorMyOrganisationProfileViewModel
        {
            RtsId = rtsId,
            Name = ctx.RtsOrganisation.Name,
            Country = ctx.RtsOrganisation.CountryName,
            Address = ctx.RtsOrganisation.Address,
            LastUpdated = ctx.SponsorOrganisation.UpdatedDate ?? ctx.SponsorOrganisation.CreatedDate ?? DateTime.MinValue
        };

        return View(model);
    }

    [Authorize(Policy = Permissions.Sponsor.MyOrganisations_Projects)]
    [HttpGet]
    public async Task<IActionResult> MyOrganisationProjects(string rtsId)
    {
        ViewBag.Active = MyOrganisationProfileOverview.Projects;

        var ctxResult = await TryGetSponsorOrgContext(rtsId);
        if (ctxResult.HasResult)
        {
            return ctxResult.Result!;
        }

        var ctx = ctxResult.Context!;

        var model = new SponsorMyOrganisationProfileViewModel
        {
            Name = ctx.RtsOrganisation.Name,
            RtsId = rtsId
        };

        return View(model);
    }

    [Authorize(Policy = Permissions.Sponsor.MyOrganisations_Users)]
    [HttpGet]
    public async Task<IActionResult> MyOrganisationUsers(string rtsId, string? searchQuery = null,
        int pageNumber = 1, int pageSize = 20, string? sortField = "GivenName", string? sortDirection = "asc")
    {
        ViewBag.Active = MyOrganisationProfileOverview.Users;

        var ctxResult = await TryGetSponsorOrgContext(rtsId);
        if (ctxResult.HasResult)
        {
            return ctxResult.Result!;
        }

        var ctx = ctxResult.Context!;
        var sponsorOrganisationDto = ctx.SponsorOrganisation;

        var model = new SponsorMyOrganisationUsersViewModel
        {
            Name = ctx.RtsOrganisation.Name,
            RtsId = rtsId,
            SponsorOrganisation = new SponsorOrganisationModel
            {
                Id = sponsorOrganisationDto.Id,
                RtsId = rtsId,
                SponsorOrganisationName = ctx.RtsOrganisation.Name,
                Countries = [ctx.RtsOrganisation.CountryName],
                IsActive = sponsorOrganisationDto.IsActive,
                UpdatedDate = sponsorOrganisationDto.UpdatedDate ?? sponsorOrganisationDto.CreatedDate,
                Users = sponsorOrganisationDto.Users
            }
        };

        var totalUserCount = 0;

        if (sponsorOrganisationDto.Users?.Any() == true)
        {
            var userIds = sponsorOrganisationDto.Users.Select(x => x.UserId.ToString());
            var users = await userService.GetUsersByIds(userIds, searchQuery, 1, int.MaxValue);
            model.Users = users.Content?.Users.Select(u => new UserViewModel(u)) ?? [];
            totalUserCount = users.Content?.TotalCount ?? 0;
        }

        model.Users = SortSponsorOrganisationUsers(model.Users, model.SponsorOrganisation.Users, sortField,
            sortDirection, pageNumber, pageSize);

        model.Pagination = BuildPagination(pageNumber, pageSize, totalUserCount, "sws:myorganisationusers",
            sortField, sortDirection,
            new Dictionary<string, string> { { "rtsId", rtsId }, { "searchQuery", searchQuery ?? string.Empty } });
        model.Pagination.SearchQuery = searchQuery;

        return View(model);
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

        // Latest DTO per user (swap g.Last() for a deterministic "latest" if you have a timestamp/sequence)
        var latestByUserId = sponsorOrganisationUserDtos?
                                 .GroupBy(x => x.UserId)
                                 .ToDictionary(g => g.Key, g => g.Last())
                             ?? new Dictionary<Guid, SponsorOrganisationUserDto>();

        var statusByUserId = latestByUserId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IsActive);
        var roleByUserId = latestByUserId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SponsorRole);
        var authoriserByUserId = latestByUserId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IsAuthoriser);

        // Optional: keep your Status mapping
        foreach (var u in list)
        {
            if (TryUserId(u, out var gid) && statusByUserId.TryGetValue(gid, out var active))
            {
                u.Status = active ? "Active" : "Disabled";
            }
        }

        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortField?.ToLowerInvariant() ?? string.Empty;

        IOrderedEnumerable<UserViewModel> ordered;

        // Primary sort
        if (field == "status")
        {
            // ASC: Active first, DESC: Disabled first
            ordered = desc
                ? list.OrderBy(IsActive) // false (disabled) first
                : list.OrderByDescending(IsActive); // true (active) first
        }
        else if (field == "sponsorrole")
        {
            ordered = desc
                ? list.OrderByDescending(GetSponsorRole, StringComparer.OrdinalIgnoreCase)
                : list.OrderBy(GetSponsorRole, StringComparer.OrdinalIgnoreCase);
        }
        else if (field == "isauthoriser")
        {
            ordered = desc
                ? list.OrderByDescending(GetIsAuthoriser)
                : list.OrderBy(GetIsAuthoriser);
        }
        else
        {
            Func<UserViewModel, string> key = field switch
            {
                "email" => x => x.Email ?? string.Empty,
                _ => x => x.GivenName ?? string.Empty
            };

            ordered = desc
                ? list.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
                : list.OrderBy(key, StringComparer.OrdinalIgnoreCase);
        }

        // Pagination
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, pageSize);

        var skip = (pageNumber - 1) * pageSize;
        return ordered.Skip(skip).Take(pageSize);

        // ---- helpers ----
        static bool TryUserId(UserViewModel u, out Guid id)
        {
            return Guid.TryParse(u.Id?.Trim(), out id);
        }

        bool IsActive(UserViewModel u)
        {
            return TryUserId(u, out var g) && statusByUserId.TryGetValue(g, out var a) && a;
        }

        string GetSponsorRole(UserViewModel u)
        {
            return TryUserId(u, out var g) && roleByUserId.TryGetValue(g, out var role)
                ? role ?? string.Empty
                : string.Empty;
        }

        bool GetIsAuthoriser(UserViewModel u)
        {
            return TryUserId(u, out var g) && authoriserByUserId.TryGetValue(g, out var a) && a;
        }
    }

    [Authorize(Policy = Permissions.Sponsor.MyOrganisations_Audit)]
    [HttpGet]
    public async Task<IActionResult> MyOrganisationAuditTrail(string rtsId)
    {
        ViewBag.Active = MyOrganisationProfileOverview.Audit;

        var ctxResult = await TryGetSponsorOrgContext(rtsId);
        if (ctxResult.HasResult)
        {
            return ctxResult.Result!;
        }

        var ctx = ctxResult.Context!;

        var auditResponse = await sponsorOrganisationService.SponsorOrganisationAuditTrail(
            rtsId, 1, int.MaxValue, "DateTimeStamp", SortDirections.Descending);

        if (!auditResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(auditResponse);
        }

        var model = new SponsorMyOrganisationAuditViewModel
        {
            RtsId = rtsId,
            Name = ctx.RtsOrganisation.Name,
            AuditTrails = auditResponse.Content!.Items.OrderByDescending(at => at.DateTimeStamp)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrganisationUsersAddUser(string rtsId, string? searchQuery)
    {
        var ctxResult = await TryGetSponsorOrgContext(rtsId);
        if (ctxResult.HasResult)
        {
            return ctxResult.Result!;
        }

        var ctx = ctxResult.Context!;

        var model = new SponsorMyOrganisationUsersViewModel
        {
            Name = ctx.RtsOrganisation.Name,
            RtsId = rtsId
        };

        if (Request.Query.ContainsKey("SearchQuery") && string.IsNullOrWhiteSpace(searchQuery))
        {
            ModelState.AddModelError("SearchQuery", "Enter a user email");
            return View(model);
        }

        if (!Request.Query.ContainsKey("SearchQuery"))
        {
            return View(model);
        }

        var usersResponse = await userService.SearchUsers(searchQuery);

        if (usersResponse is { IsSuccessStatusCode: true, Content.TotalCount: 1 })
        {
            var user = usersResponse.Content.Users.First();
            return RedirectToAction(nameof(MyOrganisationUsersAddUserRole), new { rtsId, userId = user.Id });
        }

        return RedirectToAction(nameof(MyOrganisationUsersInvalidUser), new { rtsId });
    }

    [HttpGet]
    public async Task<IActionResult> MyOrganisationUsersInvalidUser(string rtsId)
    {
        var model = new SponsorMyOrganisationUsersViewModel
        {
            RtsId = rtsId
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrganisationUsersAddUserRole(string rtsId, string userId, string? role)
    {
        var model = new SponsorMyOrganisationUsersViewModel
        {
            RtsId = rtsId
            // Populate anything else you need for the page (e.g. Name) as you already do elsewhere
        };

        // If the form has been submitted (Role exists) but nothing selected
        if (Request.Query.ContainsKey("Role") && string.IsNullOrWhiteSpace(role))
        {
            ModelState.AddModelError("Role", "Select a role");
            return View(model);
        }

        // First visit to the page (no Role in querystring)
        if (!Request.Query.ContainsKey("Role"))
        {
            return View(model);
        }

        // Role selected - continue (redirect to next step or whatever your flow is)
        return RedirectToAction(nameof(MyOrganisationUsersAddUserPermission), new { rtsId, userId, role });
    }

    [HttpGet]
    public async Task<IActionResult> MyOrganisationUsersAddUserPermission(string rtsId, string userId, string? role)
    {
        var model = new SponsorMyOrganisationUsersViewModel
        {
            RtsId = rtsId
        };

        return View(model);
    }

    [NonAction]
    private async Task<SponsorOrgContextResult> TryGetSponsorOrgContext(string rtsId)
    {
        var rtsResponse = await rtsService.GetOrganisation(rtsId);
        if (!rtsResponse.IsSuccessStatusCode)
        {
            return new SponsorOrgContextResult(null, this.ServiceError(rtsResponse));
        }

        var rbResponse = await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsId);
        if (!rbResponse.IsSuccessStatusCode)
        {
            return new SponsorOrgContextResult(null, this.ServiceError(rbResponse));
        }

        var sponsorOrganisationDto = rbResponse.Content?.SponsorOrganisations?.FirstOrDefault();
        if (sponsorOrganisationDto is null || rtsResponse.Content is null)
        {
            return new SponsorOrgContextResult(null, NotFound());
        }

        var email =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            // 403 -> /error/statuscode -> Forbidden()
            return new SponsorOrgContextResult(null, Forbid());
        }

        var inOrgAndEnabled = sponsorOrganisationDto.Users?.Any(x =>
            x.IsActive &&
            string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)
        ) == true;

        if (!inOrgAndEnabled)
        {
            // 403 -> /error/statuscode -> Forbidden()
            return new SponsorOrgContextResult(null, Forbid());
        }

        var ctx = new SponsorOrgContext(rtsId, rtsResponse.Content, sponsorOrganisationDto);
        return new SponsorOrgContextResult(ctx, null);
    }

    private sealed record SponsorOrgContext(
        string RtsId,
        OrganisationDto RtsOrganisation,
        SponsorOrganisationDto SponsorOrganisation);

    private sealed record SponsorOrgContextResult(
        SponsorOrgContext? Context,
        IActionResult? Result)
    {
        public bool HasResult => Result is not null;
    }
}