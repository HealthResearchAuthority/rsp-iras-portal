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
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Controllers;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class MyOrganisationsController(
    ISponsorOrganisationService sponsorOrganisationService,
    IRtsService rtsService
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
            UserId = Guid.Parse(userId!)
        };

        var response = await sponsorOrganisationService.GetAllSponsorOrganisations(
            request, 1, int.MaxValue, sortField, sortDirection);

        var items = response.Content?.SponsorOrganisations ?? Enumerable.Empty<SponsorOrganisationDto>();

        model.MyOrganisations = SortSponsorOrganisations(items, sortField, sortDirection).ToList();
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
    public async Task<IActionResult> MyOrganisationProfile()
    {
        return View();
    }

    // Sorting extracted and made stable/consistent4
    [NonAction]
    private static IEnumerable<SponsorOrganisationDto> SortSponsorOrganisations(
        IEnumerable<SponsorOrganisationDto> items,
        string? sortField,
        string? sortDirection)
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

            _ => desc
                ? items.OrderByDescending(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
        };

        return sorted.ToList();
    }
}