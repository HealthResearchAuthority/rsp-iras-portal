using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Services;

public interface ISponsorUserAuthorisationService
{
    Task<SponsorUserAuthorisationResult> AuthoriseAsync(
        Controller controller,
        Guid sponsorOrganisationUserId,
        ClaimsPrincipal user);
}

public sealed class SponsorUserAuthorisationService : ISponsorUserAuthorisationService
{
    private readonly IUserManagementService _userService;
    private readonly ISponsorOrganisationService _sponsorOrganisationService;

    public SponsorUserAuthorisationService(
        IUserManagementService userService,
        ISponsorOrganisationService sponsorOrganisationService)
    {
        _userService = userService;
        _sponsorOrganisationService = sponsorOrganisationService;
    }

    public async Task<SponsorUserAuthorisationResult> AuthoriseAsync(
        Controller controller,
        Guid sponsorOrganisationUserId,
        ClaimsPrincipal user)
    {
        var currentUserEmail = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(currentUserEmail))
        {
            var errorResponse = new ServiceResponse<UserResponse>()
                .WithError("Missing email identifier for the current user.", "MissingUserEmail", HttpStatusCode.BadRequest);

            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(errorResponse));
        }

        var userEntityResponse = await _userService.GetUser(null, currentUserEmail);

        if (!userEntityResponse.IsSuccessStatusCode)
            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(userEntityResponse));

        if (!Guid.TryParse(userEntityResponse.Content?.User.Id?.Trim(), out var gid))
        {
            var errorResponse = new ServiceResponse<UserResponse>()
                .WithError("Invalid or missing user identifier for the current user.", "InvalidUserId", HttpStatusCode.BadRequest);

            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(errorResponse));
        }

        var sponsorOrganisationsResponse =
            await _sponsorOrganisationService.GetAllActiveSponsorOrganisationsForEnabledUser(gid);

        if (!sponsorOrganisationsResponse.IsSuccessStatusCode)
            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(sponsorOrganisationsResponse));

        var membershipId = sponsorOrganisationsResponse.Content?
            .SingleOrDefault()?
            .Users?
            .SingleOrDefault(u => u.UserId == gid)?
            .Id;

        if (membershipId is null || sponsorOrganisationUserId != membershipId.Value)
            return SponsorUserAuthorisationResult.Fail(controller.Forbid());

        return SponsorUserAuthorisationResult.Ok(gid);
    }
}