using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Controllers;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("[action]", Name = "sws:[action]")]
public class SponsorWorkspaceController(
    IUserManagementService userService,
    ISponsorOrganisationService sponsorOrganisationService
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> SponsorWorkspace()
    {
        var currentUserEmail = HttpContext?.User.FindFirstValue(ClaimTypes.Email);
        var userEntityResponse = await userService.GetUser(null, currentUserEmail);

        if (!userEntityResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(userEntityResponse);
        }

        var gid = Guid.Parse(userEntityResponse.Content.User.Id.Trim());

        var sponsorOrganisationsResponse =
            await sponsorOrganisationService.GetAllActiveSponsorOrganisationsForEnabledUser(gid);

        if (!sponsorOrganisationsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(sponsorOrganisationsResponse);
        }

        var activeSponsorOrganisations = sponsorOrganisationsResponse.Content?
            .Where(o => o.IsActive)
            .ToList();

        ViewBag.SponsorOrganisationUserId = activeSponsorOrganisations
            .SelectMany(o => o.Users ?? Enumerable.Empty<SponsorOrganisationUserDto>())
            .FirstOrDefault(u => u.UserId == gid)?.Id ?? Guid.Empty;

        // SINGLE VIEW
        return View();
    }
}