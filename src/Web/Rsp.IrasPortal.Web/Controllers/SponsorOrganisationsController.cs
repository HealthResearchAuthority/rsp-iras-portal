using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "soc:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
public class SponsorOrganisationsController(
    ISponsorOrganisationService sponsorOrganisationService
    //IUserManagementService userService,
    //IValidator<AddUpdateSponsorOrganisationModel> validator
)
    : Controller
{
    private const string UpdateMode = "update";
    private const string CreateMode = "create";
    private const string DisableMode = "disable";
    private const string EnableMode = "enable";

    /// <summary>
    ///     Displays a list of review bodies
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
            // RESET ON SEARCH AND REMOVE FILTERS
            pageNumber = 1;
            pageSize = 20;
        }

        model ??= new SponsorOrganisationSearchViewModel();

        // Always attempt to restore from session if nothing is currently set
        if (HttpContext.Request.Method == HttpMethods.Get)
        {
            var savedSearch = HttpContext.Session.GetString(SessionKeys.SponsorOrganisationsSearch);
            if (!string.IsNullOrWhiteSpace(savedSearch))
            {
                model.Search = JsonSerializer.Deserialize<SponsorOrganisationSearchModel>(savedSearch);
            }
        }

        var request = new SponsorOrganisationSearchRequest
        {
            SearchQuery = model.Search.SearchQuery,
            Country = model.Search.Country,
            Status = model.Search.Status
        };

        var response =
            await sponsorOrganisationService.GetAllSponsorOrganisations(request, pageNumber, pageSize, sortField,
                sortDirection);

        var paginationModel = new PaginationViewModel(pageNumber, pageSize, response.Content?.TotalCount ?? 0)
        {
            RouteName = "soc:viewsponsororganisations",
            SortField = sortField,
            SortDirection = sortDirection
        };

        var reviewBodySearchViewModel = new SponsorOrganisationSearchViewModel
        {
            Pagination = paginationModel,
            SponsorOrganisations = response.Content?.SponsorOrganisations,
            Search = model.Search
        };

        // Save applied filters to session
        // Only persist if search has any real values
        if (!string.IsNullOrWhiteSpace(model.Search.SearchQuery) ||
            model.Search.Country.Count > 0 ||
            model.Search.Status.HasValue
           )
        {
            HttpContext.Session.SetString(SessionKeys.SponsorOrganisationsSearch,
                JsonSerializer.Serialize(model.Search));
        }

        return View("ViewSponsorOrganisations", reviewBodySearchViewModel);
    }

    [Route("/sponsororganisation/applyfilters", Name = "soc:applyfilters")]
    [HttpPost]
    [HttpGet]
    [CmsContentAction(nameof(Index))]
    public async Task<IActionResult> ApplyFilters(
        SponsorOrganisationSearchViewModel model,
        string? sortField = nameof(UserViewModel.GivenName),
        string? sortDirection = SortDirections.Ascending,
        [FromQuery] bool fromPagination = false)
    {
        // Always attempt to restore from session if nothing is currently set
        if (HttpContext.Request.Method == HttpMethods.Get)
        {
            var savedSearch = HttpContext.Session.GetString(SessionKeys.SponsorOrganisationsSearch);
            if (!string.IsNullOrWhiteSpace(savedSearch))
            {
                model.Search = JsonSerializer.Deserialize<SponsorOrganisationSearchModel>(savedSearch);
            }
        }

        // Call Index with matching parameter set
        return await Index(
            1, // pageNumber
            20, // pageSize
            sortField,
            sortDirection,
            model,
            fromPagination);
    }
}