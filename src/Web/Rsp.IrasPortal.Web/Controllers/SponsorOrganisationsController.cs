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
)
    : Controller
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

        string CountriesKey(SponsorOrganisationDto x)
        {
            return x.Countries == null || !x.Countries.Any()
                ? string.Empty
                : string.Join(", ", x.Countries.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
        }

        var items = response.Content?.SponsorOrganisations ?? Enumerable.Empty<SponsorOrganisationDto>();
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

            "status" => desc
                ? items.OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.IsActive)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase),

            _ => desc
                ? items.OrderByDescending(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
        };

        var sortedPage = sorted.ToList(); // keep current paging

        var paginationModel = new PaginationViewModel(pageNumber, pageSize, response.Content?.TotalCount ?? 0)
        {
            RouteName = "soc:viewsponsororganisations",
            SortField = sortField,
            SortDirection = sortDirection
        };

        var sponsorOrganisationSearchViewModel = new SponsorOrganisationSearchViewModel
        {
            Pagination = paginationModel,
            SponsorOrganisations = sortedPage,
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

        return View("ViewSponsorOrganisations", sponsorOrganisationSearchViewModel);
    }

    [Route("/sponsororganisations/applyfilters", Name = "soc:applyfilters")]
    [HttpPost]
    [HttpGet]
    [CmsContentAction(nameof(Index))]
    public async Task<IActionResult> ApplyFilters(
        SponsorOrganisationSearchViewModel model,
        string? sortField = "name",
        string? sortDirection = "asc",
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

    /// <summary>
    ///     Displays the empty review body to create
    /// </summary>
    [HttpGet]
    [Route("/sponsororganisations/setup", Name = "soc:setupsponsororganisation")]
    public IActionResult SetupSponsorOrganisation()
    {
        var model = new SponsorOrganisationSetupViewModel();
        return View("SetupSponsorOrganisation", model);
    }

    /// <summary>
    ///     Check sponsor organisation details
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
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
            var organisation = model.SponsorOrganisation ?? model.SponsorOrgSearch.SelectedOrganisation;

            var rtsNameSearch =
                await rtsService.GetOrganisationsByName(organisation, null, 1, int.MaxValue);

            if (rtsNameSearch.IsSuccessStatusCode)
            {
                var rtsOrganisationDto = rtsNameSearch.Content?.Organisations.FirstOrDefault();

                if (rtsOrganisationDto != null)
                {
                    var organisationByName =
                        await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsOrganisationDto.Id);

                    if (organisationByName.IsSuccessStatusCode)
                    {
                        if (organisationByName.Content.TotalCount > 0)
                        {
                            ModelState.AddModelError("SponsorOrganisation",
                                "A sponsor organisation with this name already exists");
                        }
                        else
                        {
                            return RedirectToAction("ConfirmSponsorOrganisation", new SponsorOrganisationModel
                            {
                                SponsorOrganisationName = rtsOrganisationDto.Name,
                                Countries = [rtsOrganisationDto.CountryName],
                                RtsId = rtsOrganisationDto.Id
                            });
                        }
                    }
                    else
                    {
                        return this.ServiceError(organisationByName);
                    }
                }
                else
                {
                    TempData[TempDataKeys.ShowNoResultsFound] = true;
                }
            }
        }

        return View("SetupSponsorOrganisation", model);
    }

    /// <summary>
    ///     Displays the empty review body to create
    /// </summary>
    [HttpGet]
    [Route("/sponsororganisations/confirm", Name = "soc:sponsororganisation")]
    public IActionResult ConfirmSponsorOrganisation(SponsorOrganisationModel model)
    {
        return View("ConfirmSponsorOrganisation", model);
    }

    /// <summary>
    ///     Check sponsor organisation details
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/sponsororganisations/save", Name = "soc:savesponsororganisation")]
    public async Task<IActionResult> SaveSponsorOrganisation(SponsorOrganisationModel model)
    {
        model.CreatedBy = User?.Identity?.Name!;
        model.CreatedDate = DateTime.UtcNow;

        var sponsorOrganisationDto = model.Adapt<SponsorOrganisationDto>();

        var response =
            await sponsorOrganisationService.CreateSponsorOrganisation(sponsorOrganisationDto);

        if (response.IsSuccessStatusCode)
        {
            TempData[TempDataKeys.ShowNotificationBanner] = true;
            return RedirectToAction("Index");
        }

        // return error page as api wasn't successful
        return this.ServiceError(response);
    }

    /// <summary>
    ///     Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination. Defults to 5 if not provided.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> SearchOrganisations(SponsorOrganisationSetupViewModel model, string? role,
        int? pageSize = 5, int pageIndex = 1)
    {
        var returnUrl = TempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        // set the previous, current and next stages
        TempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");

        // when search is performed, empty the currently selected organisation
        model.SponsorOrgSearch.SelectedOrganisation = string.Empty;
        TempData.TryAdd(TempDataKeys.OrgSearch, model.SponsorOrgSearch, true);

        if (string.IsNullOrEmpty(model.SponsorOrgSearch.SearchText) || model.SponsorOrgSearch.SearchText.Length < 3)
        {
            ModelState.AddModelError("sponsor_org_search",
                "Please provide 3 or more characters to search sponsor organisation.");

            // save the model state in temp data, to use it on redirects to show validation errors
            // the modelstate will be merged using the action filter ModelStateMergeAttribute
            // only if the TempData has ModelState stored
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);

            // Return the view with the model state errors.
            return Redirect(returnUrl!);
        }

        // Use the default sponsor role if no role is provided.
        role ??= OrganisationRoles.Sponsor;

        var searchResponse =
            await rtsService.GetOrganisationsByName(model.SponsorOrgSearch.SearchText, role, pageIndex, pageSize);

        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return this.ServiceError(searchResponse);
        }

        var sponsorOrganisations = searchResponse.Content;

        TempData.TryAdd(TempDataKeys.SponsorOrganisations, sponsorOrganisations, true);

        return Redirect(returnUrl!);
    }

    /// <summary>
    ///     Displays a single review body
    /// </summary>
    [HttpGet]
    [Route("/sponsororganisations/view", Name = "soc:viewsponsororganisation")]
    public async Task<IActionResult> ViewSponsorOrganisation(string rtsId)
    {
        var response =
            await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsId);
        if (response.IsSuccessStatusCode)
        {
            if (response.Content.SponsorOrganisations.Any())
            {
                var sponsorOrganisationDto = response.Content.SponsorOrganisations.ToList()[0];
                var organisationDto = await rtsService.GetOrganisation(rtsId);

                if (organisationDto.IsSuccessStatusCode)
                {
                    var sponsorOrganisationModel = new SponsorOrganisationModel
                    {
                        RtsId = rtsId,
                        SponsorOrganisationName = organisationDto.Content.Name,
                        Countries = [organisationDto.Content.CountryName],
                        IsActive = sponsorOrganisationDto.IsActive,
                        UpdatedDate = sponsorOrganisationDto.UpdatedDate ?? sponsorOrganisationDto.CreatedDate
                    };


                    return View(sponsorOrganisationModel);
                }
            }
        }
        else
        {
            return this.ServiceError(response);
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    ///     Displays users for a review body
    /// </summary>
    [HttpGet]
    [Route("/sponsororganisations/viewusers", Name = "soc:viewsponsororganisationusers")]
    public async Task<IActionResult> ViewSponsorOrganisationUsers(string rtsId, string? searchQuery = null,
        int pageNumber = 1, int pageSize = 20)
    {
        var response =
            await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsId);

        if (response.IsSuccessStatusCode)
        {
            if (response.Content.SponsorOrganisations.Any())
            {
                var sponsorOrganisationDto = response.Content.SponsorOrganisations.ToList()[0];
                var organisationDto = await rtsService.GetOrganisation(rtsId);

                if (organisationDto.IsSuccessStatusCode)
                {
                    var sponsorOrganisationModel = new SponsorOrganisationModel
                    {
                        RtsId = rtsId,
                        SponsorOrganisationName = organisationDto.Content.Name,
                        Countries = [organisationDto.Content.CountryName],
                        IsActive = sponsorOrganisationDto.IsActive,
                        UpdatedDate = sponsorOrganisationDto.UpdatedDate ?? sponsorOrganisationDto.CreatedDate
                    };

                    var totalUserCount = 0;
                    var model = new SponsorOrganisationListUsersModel
                    {
                        SponsorOrganisation = sponsorOrganisationModel!
                    };

                    if (sponsorOrganisationDto?.Users != null)
                    {
                        var userIds = sponsorOrganisationDto?.Users?.Select(x => x.UserId.ToString());
                        if (userIds != null && userIds.Any())
                        {
                            var users = await userService.GetUsersByIds(userIds,
                                searchQuery,
                                pageNumber,
                                pageSize);

                            model.Users = users.Content?.Users.Select(user => new UserViewModel(user)) ?? [];

                            totalUserCount = users.Content?.TotalCount ?? 0;
                        }
                    }

                    model.Pagination = new PaginationViewModel(pageNumber, pageSize, totalUserCount)
                    {
                        SearchQuery = searchQuery,
                        RouteName = "soc:viewsponsororganisationusers",
                        AdditionalParameters =
                        {
                            { "rtsId", rtsId }
                        }
                    };

                    return View(model);
                }
            }
        }

        return this.ServiceError(response);
    }

    [HttpGet]
    public async Task<IActionResult> ViewAddUser(string rtsId, string? searchQuery = null, int pageNumber = 1,
        int pageSize = 20)
    {
        var response =
            await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsId);

        if (response.IsSuccessStatusCode)
        {
            if (response.Content.SponsorOrganisations.Any())
            {
                var sponsorOrganisationDto = response.Content.SponsorOrganisations.ToList()[0];
                var organisationDto = await rtsService.GetOrganisation(rtsId);

                if (organisationDto.IsSuccessStatusCode)
                {
                    var sponsorOrganisationModel = new SponsorOrganisationModel
                    {
                        RtsId = rtsId,
                        SponsorOrganisationName = organisationDto.Content.Name,
                        Countries = [organisationDto.Content.CountryName],
                        IsActive = sponsorOrganisationDto.IsActive,
                        UpdatedDate = sponsorOrganisationDto.UpdatedDate ?? sponsorOrganisationDto.CreatedDate
                    };

                    var totalUserCount = 0;
                    var model = new SponsorOrganisationListUsersModel
                    {
                        SponsorOrganisation = sponsorOrganisationModel!
                    };

                    if (sponsorOrganisationDto?.Users != null)
                    {
                        var existingUserIds = sponsorOrganisationDto?.Users?.Select(x => x.UserId.ToString()) ?? [];

                        if (!string.IsNullOrEmpty(searchQuery))
                        {
                            // search all users
                            var users = await userService.SearchUsers(searchQuery, existingUserIds, pageNumber,
                                pageSize);

                            model.Users = users.Content?.Users.Select(user => new UserViewModel(user)) ?? [];

                            model.Pagination =
                                new PaginationViewModel(pageNumber, pageSize, users.Content?.TotalCount ?? 0)
                                {
                                    RouteName = "soc:viewadduser",
                                    SearchQuery = searchQuery,
                                    AdditionalParameters =
                                    {
                                        { "rtsId", rtsId }
                                    }
                                };
                        }
                    }

                    return View(model);
                }
            }
        }

        return this.ServiceError(response);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmAddUpdateUser(string rtsId, Guid userId)
    {
        var response =
            await sponsorOrganisationService.GetSponsorOrganisationByRtsId(rtsId);

        if (response.IsSuccessStatusCode)
        {
            if (response.Content.SponsorOrganisations.Any())
            {
                var sponsorOrganisationDto = response.Content.SponsorOrganisations.ToList()[0];
                var organisationDto = await rtsService.GetOrganisation(rtsId);

                if (organisationDto.IsSuccessStatusCode)
                {
                    var sponsorOrganisationModel = new SponsorOrganisationModel
                    {
                        Id = sponsorOrganisationDto.Id,
                        RtsId = rtsId,
                        SponsorOrganisationName = organisationDto.Content.Name,
                        Countries = [organisationDto.Content.CountryName],
                        IsActive = sponsorOrganisationDto.IsActive,
                        UpdatedDate = sponsorOrganisationDto.UpdatedDate ?? sponsorOrganisationDto.CreatedDate
                    };

                    // get selected user
                    var user = await userService.GetUser(userId.ToString(), null);

                    if (user.IsSuccessStatusCode)
                    {
                        var model = new ConfirmAddUpdateSponsorOrganisationUserModel
                        {
                            SponsorOrganisation = sponsorOrganisationModel,
                            User = user.Content != null ? new UserViewModel(user.Content) : new UserViewModel()
                        };

                        TempData[TempDataKeys.ShowEditLink] = false;

                        return View("ConfirmAddUpdateUser", model);
                    }
                }
            }
        }

        return this.ServiceError(response);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitAddUser(string rtsId, Guid userId, Guid sponsorOrganisationId)
    {
        // get selected user
        var user = await userService.GetUser(userId.ToString(), null);

        var sponsorOrganisationUserDto = new SponsorOrganisationUserDto
        {
            Id = sponsorOrganisationId,
            RtsId = rtsId,
            UserId = userId,
            Email = user.Content?.User.Email,
            DateAdded = DateTime.UtcNow
        };

        //AddUpdateReviewBodyMode
        var response = await sponsorOrganisationService.AddUserToSponsorOrganisation(sponsorOrganisationUserDto);

        if (response.IsSuccessStatusCode)
        {
            // user was created succesfully so let's assign them the 'sponsor' role
            await userService.UpdateRoles(user.Content.User.Email, null, "sponsor");

            // SHOW BANNER ON NEXT VIEW
            TempData[TempDataKeys.ShowNotificationBanner] = true;

            return RedirectToAction("ViewSponsorOrganisationUsers", "SponsorOrganisations", new
            {
                rtsId
            });
        }

        return this.ServiceError(response);
    }
}