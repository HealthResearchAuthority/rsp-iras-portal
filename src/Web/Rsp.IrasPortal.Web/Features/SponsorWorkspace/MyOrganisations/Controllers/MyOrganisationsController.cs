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

        model.MyOrganisations = items.SortSponsorOrganisations(sortField, sortDirection).ToList();
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
        var rtsResponse = await rtsService.GetOrganisation(rtsId);

        if (!rtsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(rtsResponse);
        }

        var model = new SponsorMyOrganisationProfileViewModel()
        {
            Name = rtsResponse.Content?.Name,
            RtsId = rtsId
        };

        return View(model);
    }
}