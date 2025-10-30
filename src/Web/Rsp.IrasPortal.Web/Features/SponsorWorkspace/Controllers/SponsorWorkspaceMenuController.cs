using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
[Authorize(Policy = "IsSponsor")]
public class SponsorWorkspaceMenuController(IUserManagementService userService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> SponsorWorkspaceMenu()
    {
        var currentUserEmail = HttpContext?.User.FindFirstValue(ClaimTypes.Email);
        var userEntityResponse = await userService.GetUser(null, currentUserEmail);

        if (!userEntityResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(userEntityResponse);
        }

        if (string.IsNullOrEmpty(userEntityResponse.Content?.User.Organisation))
        {
            throw new InvalidOperationException("Sponsor organisation name is missing for the current user.");
        }

        ViewBag.SponsorOrganisationName = userEntityResponse.Content?.User.Organisation;

        return View();
    }
}