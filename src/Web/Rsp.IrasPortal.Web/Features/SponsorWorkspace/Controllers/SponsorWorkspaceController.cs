using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Route("[action]", Name = "sws:[action]")]
[Authorize(Policy = "IsSponsor")]
public class SponsorWorkspaceController(IUserManagementService userService) : Controller
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

        if (string.IsNullOrEmpty(userEntityResponse.Content?.User.Organisation))
        {
            var errorResponse = new ServiceResponse<UserResponse>()
                        .WithError(
                            errorMessage: "Sponsor organisation name is missing for the current user.",
                            reasonPhrase: "InvalidOperation",
                            statusCode: HttpStatusCode.BadRequest
                        );

            return this.ServiceError(errorResponse);
        }

        ViewBag.SponsorOrganisationName = userEntityResponse.Content?.User.Organisation;

        return View();
    }
}