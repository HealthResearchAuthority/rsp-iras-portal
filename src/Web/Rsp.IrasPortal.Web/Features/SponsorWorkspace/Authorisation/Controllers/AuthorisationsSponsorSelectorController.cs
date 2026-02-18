using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Services;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class AuthorisationsSponsorSelectorController(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ISponsorOrganisationService sponsorOrganisationService,
    ICmsQuestionsetService cmsQuestionsetService,
    IRtsService rtsService,
    ISponsorUserAuthorisationService sponsorUserAuthorisationService
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, null!)
{
    [Authorize(Policy = Permissions.Sponsor.Modifications_Search)]
    [HttpGet]
    public async Task<IActionResult> SponsorSelector
    (
        Guid sponsorOrganisationUserId
    )
    {
        var auth = await sponsorUserAuthorisationService.AuthoriseAsync(this, sponsorOrganisationUserId, User);
        if (!auth.IsAuthorised)
        {
            return auth.FailureResult!;
        }

        var model = new AuthorisationsSponsorSelectorViewModel();

        var response =
            await sponsorOrganisationService.GetAllActiveSponsorOrganisationsForEnabledUser(auth.CurrentUserId.Value);

        if (response.IsSuccessStatusCode)
        {
            var sponsorOrganisations = response.Content
                .Where(o => o.IsActive && o.Users != null &&
                            o.Users.Any(u =>
                                u.UserId == auth.CurrentUserId.Value &&
                                u.IsAuthoriser));

            foreach (var sponsorOrganisation in sponsorOrganisations)
            {
                var sponsor = await rtsService.GetOrganisation(sponsorOrganisation.RtsId);

                if (sponsor.IsSuccessStatusCode)
                {
                    sponsorOrganisation.SponsorOrganisationName = sponsor.Content.Name;
                }
            }

            switch (sponsorOrganisations?.Count())
            {
                case 0:
                    {
                        return Forbid();
                    }
                case 1: // REDIRECT TO MODIFICATIONS VIEW ONLY
                    return RedirectToRoute("sws:modifications", new
                    {
                        sponsorOrganisationUserId,
                        rtsId = sponsorOrganisations.First().RtsId
                    });

                default: // MULTI SPONSOR
                    {
                        model.SponsorOrganisations = sponsorOrganisations;
                        model.SponsorOrganisationUserId = sponsorOrganisationUserId;
                        return View(model);
                    }
            }
        }

        return this.ServiceError(response);
    }
}