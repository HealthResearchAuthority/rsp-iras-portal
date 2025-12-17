using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Controllers;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class MyOrganisationsController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, null!)
{
    [Authorize(Policy = Permissions.Sponsor.MyOrganisations_Search)]
    [HttpGet]
    public async Task<IActionResult> MyOrganisations
    (
        Guid sponsorOrganisationUserId,
        string sortField = nameof(SponsorMyOrganisationModel.SponsorOrganisationName),
        string sortDirection = SortDirections.Descending
    )
    {
        var model = new SponsorMyOrganisationsViewModel();

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.SponsorMyOrganisationsSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<SponsorMyOrganisationsSearchModel>(json)!;
        }

        var searchQuery = new SponsorMyOrganisationsSearchModel
        {
            SearchTerm = model.Search.SearchTerm
        };

        // getting modifications by sponsor organisation name

        model.SponsorOrganisationUserId = sponsorOrganisationUserId;

        return View(model);
    }
}