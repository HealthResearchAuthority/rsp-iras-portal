using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.Controllers;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("[action]", Name = "sws:[action]")]
public class SponsorWorkspaceController
(
    IUserManagementService userService,
    ISponsorOrganisationService sponsorOrganisationService,
    IRtsService rtsService
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
        if (!Guid.TryParse(userEntityResponse.Content!.User.Id?.Trim(), out var gid))
        {
            var errorResponse = new ServiceResponse<UserResponse>()
                    .WithError(
                        errorMessage: "Invalid or missing user identifier for the current user.",
                        reasonPhrase: "InvalidUserId",
                        statusCode: HttpStatusCode.BadRequest
                    );
            return this.ServiceError(errorResponse);
        }

        var sponsorOrganisationsResponse = await sponsorOrganisationService.GetAllActiveSponsorOrganisationsForEnabledUser(gid);

        if (!sponsorOrganisationsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(sponsorOrganisationsResponse);
        }

        return View();
    }
}