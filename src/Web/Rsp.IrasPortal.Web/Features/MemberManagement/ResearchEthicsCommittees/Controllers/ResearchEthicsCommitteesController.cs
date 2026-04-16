using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

namespace Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Controllers;

/// <summary>
///     Controller responsible for handling member management workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.MemberManagement)]
[Route("membermanagement/[action]", Name = "mm:[action]")]
[FeatureGate(FeatureFlags.RecMemberManagement)]
public class ResearchEthicsCommitteesController : Controller
{
    [Authorize(Policy = Permissions.MemberManagement.ResearchEthicsCommittees_Search)]
    [HttpGet]
    public async Task<IActionResult> ResearchEthicsCommittees
    (
        string sortField = "",
        string sortDirection = SortDirections.Ascending
    )
    {
        var model = new MemberManagementResearchEthicsCommitteesViewModel();


        return View(model);
    }


    [Route("/sponsorworkspace/searchresearchethicscommittees", Name = "mm:searchresearchethicscommittees")]
    [HttpPost]
    [CmsContentAction(nameof(Index))]
    public Task<IActionResult> SearchMyOrganisations(
        MemberManagementResearchEthicsCommitteesViewModel model,
        string? sortField = "ResearchEthicsCommitteeName",
        string? sortDirection = "asc")
    {
        HttpContext.Session.SetString(
            SessionKeys.MemberManagementResearchEthicsCommitteesSearch,
            JsonSerializer.Serialize(model.Search ?? new MemberManagementResearchEthicsCommitteesSearchModel()));

        // PRG: redirect to Index with query params (no model in body)
        IActionResult result = RedirectToAction(nameof(ResearchEthicsCommittees), new
        {
            sortField,
            sortDirection
        });

        return Task.FromResult(result);
    }
}