using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Application.Constants;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;

namespace Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Controllers;

/// <summary>
///     Controller responsible for handling member management workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.MemberManagement)]
[Route("membermanagement/[action]", Name = "mm:[action]")]
[FeatureGate(FeatureFlags.RecMemberManagement)]
public class ResearchEthicsCommitteesController(
    IReviewBodyService reviewBodyService,
    IUserManagementService userService) : Controller
{
    /// <summary>
    ///     Displays a list of Research Ethics Committees
    /// </summary>
    [Authorize(Policy = Permissions.MemberManagement.ResearchEthicsCommittees_Search)]
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> ResearchEthicsCommittees(
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ReviewBodyDto.RegulatoryBodyName),
        string? sortDirection = SortDirections.Ascending,
        [FromForm] MemberManagementResearchEthicsCommitteesViewModel? model = null,
        [FromQuery] bool fromPagination = false)
    {
        if (!fromPagination)
        {
            pageNumber = 1;
            pageSize = 20;
        }

        model ??= new MemberManagementResearchEthicsCommitteesViewModel();

            // Always attempt to restore from session if nothing is currently set
        if (HttpContext.Request.Method == HttpMethods.Get)
        {
            var savedSearch = HttpContext.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
            if (!string.IsNullOrWhiteSpace(savedSearch))
            {
                model.Search = JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(savedSearch);
            }
        }

        var userId = (HttpContext.Items[ContextItemKeys.UserId] as string)!;
        var user = await userService.GetUser(userId, null);
        var countries = user.Content.User.Country.Split(",").ToList();

        var request = new ReviewBodySearchRequest
        {
            SearchQuery = model.Search.SearchTerm,
            Country = countries,
            ReviewBodyType = [ReviewBodyType.ResearchEthicsCommittee]
        };

        var response =
            await reviewBodyService.GetAllReviewBodies(request, pageNumber, pageSize, sortField, sortDirection);

        var paginationModel = new PaginationViewModel(pageNumber, pageSize, response.Content?.TotalCount ?? 0)
        {
            RouteName = "mm:researchethicscommittees",
            SortField = sortField,
            SortDirection = sortDirection
        };

        var managementResearchEthicsCommitteesViewModel = new MemberManagementResearchEthicsCommitteesViewModel
        {
            Pagination = paginationModel,
            ResearchEthicsCommittees = response.Content?.ReviewBodies,
            Search = model.Search
        };

        // Save applied filters to session
        // Only persist if search has any real values
   
            HttpContext.Session.SetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch, JsonSerializer.Serialize(model.Search));
        

        return View(managementResearchEthicsCommitteesViewModel);
    }

    [Route("/reviewbody/applyfilters", Name = "rbc:applyfilters")]
    [HttpPost]
    [HttpGet]
    [CmsContentAction(nameof(ResearchEthicsCommittees))]
    public async Task<IActionResult> ApplyFilters(
        MemberManagementResearchEthicsCommitteesViewModel model,
        string? sortField = nameof(ReviewBodyDto.RegulatoryBodyName),
        string? sortDirection = SortDirections.Ascending,
        [FromQuery] bool fromPagination = false)
    {
        // Always attempt to restore from session if nothing is currently set
        if (HttpContext.Request.Method == HttpMethods.Get)
        {
            var savedSearch = HttpContext.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
            if (!string.IsNullOrWhiteSpace(savedSearch))
            {
                model.Search = JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(savedSearch);
            }
        }

        // Call Index with matching parameter set
        return await ResearchEthicsCommittees(
            1, // pageNumber
            20, // pageSize
            sortField,
            sortDirection,
            model,
            fromPagination);
    }

    /// <summary>
    ///     Displays a Research ethics committee profile
    /// </summary>
    [Authorize(Policy = Permissions.MemberManagement.ResearchEthicsCommittees_Access)]
    [HttpGet]
    public async Task<IActionResult> ResearchEthicsCommitteesProfile(Guid id)
    {
        var reviewBody = await reviewBodyService.GetReviewBodyById(id);

        if (!await MemberManagementHelper.UserHasAccess(reviewBody.Content, User, userService))
        {
            // if user does not have access to the review body, return 403 forbidden
            return Forbid();
        }

        var model = reviewBody.Content.Adapt<MemberManagementResearchEthicsCommitteesProfileViewModel>();

        return View(model);
    }
}