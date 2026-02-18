using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
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


    Task<SponsorUserAuthorisationResult> AuthoriseWithOrganisationContextAsync(
        Controller controller,
        Guid sponsorOrganisationUserId,
        ClaimsPrincipal user,
        string rtsId);
}

public sealed class SponsorUserAuthorisationService : ISponsorUserAuthorisationService
{
    private readonly IRtsService _rtsService;
    private readonly ISponsorOrganisationService _sponsorOrganisationService;
    private readonly IUserManagementService _userService;

    public SponsorUserAuthorisationService(
        IUserManagementService userService,
        ISponsorOrganisationService sponsorOrganisationService)
    {
        _userService = userService;
        _sponsorOrganisationService = sponsorOrganisationService;
    }


    public SponsorUserAuthorisationService(
        IUserManagementService userService,
        ISponsorOrganisationService sponsorOrganisationService,
        IRtsService rtsService)
    {
        _userService = userService;
        _sponsorOrganisationService = sponsorOrganisationService;
        _rtsService = rtsService;
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
                .WithError("Missing email identifier for the current user.", "MissingUserEmail",
                    HttpStatusCode.BadRequest);

            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(errorResponse));
        }

        var userEntityResponse = await _userService.GetUser(null, currentUserEmail);

        if (!userEntityResponse.IsSuccessStatusCode)
        {
            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(userEntityResponse));
        }

        if (!Guid.TryParse(userEntityResponse.Content?.User.Id?.Trim(), out var gid))
        {
            var errorResponse = new ServiceResponse<UserResponse>()
                .WithError("Invalid or missing user identifier for the current user.", "InvalidUserId",
                    HttpStatusCode.BadRequest);

            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(errorResponse));
        }

        var sponsorOrganisationsResponse =
            await _sponsorOrganisationService.GetAllActiveSponsorOrganisationsForEnabledUser(gid);

        if (!sponsorOrganisationsResponse.IsSuccessStatusCode)
        {
            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(sponsorOrganisationsResponse));
        }

        var hasActiveSponsorOrganisation =
            sponsorOrganisationsResponse.Content?.Any(o => o.IsActive) == true;

        if (!hasActiveSponsorOrganisation)
        {
            return SponsorUserAuthorisationResult.Fail(controller.Forbid());
        }

        var userId = sponsorOrganisationsResponse.Content?
            .FirstOrDefault()?
            .Users?
            .FirstOrDefault(u => u.UserId == gid)?
            .Id;

        if (sponsorOrganisationUserId != userId)
        {
            return SponsorUserAuthorisationResult.Fail(controller.Forbid());
        }

        return SponsorUserAuthorisationResult.Ok(gid);
    }


    public async Task<SponsorUserAuthorisationResult> AuthoriseWithOrganisationContextAsync(
        Controller controller,
        Guid sponsorOrganisationUserId,
        ClaimsPrincipal user,
        string rtsId)
    {
        var auth = await AuthoriseAsync(controller, sponsorOrganisationUserId, user);
        if (!auth.IsAuthorised)
        {
            return auth;
        }

        // Load sponsor orgs once and filter to authoriser-capable active orgs
        var response =
            await _sponsorOrganisationService.GetAllActiveSponsorOrganisationsForEnabledUser(auth.CurrentUserId!.Value);

        if (!response.IsSuccessStatusCode)
        {
            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(response));
        }

        var sponsorOrganisations = (response.Content ?? Enumerable.Empty<SponsorOrganisationDto>())
            .Where(o => o.IsActive
                        && o.Users != null
                        && o.Users.Any(u => u.UserId == auth.CurrentUserId.Value && u.IsAuthoriser))
            .ToList();

        // keep count/list on result for the controller/viewmodel
        auth.WithSponsorOrganisations(sponsorOrganisations);

        // ensure the requested rtsId is allowed for this user
        if (!sponsorOrganisations.Any(x => x.RtsId == rtsId))
        {
            return SponsorUserAuthorisationResult.Fail(controller.Forbid());
        }

        // load name for the selected rts org
        var rtsResponse = await _rtsService.GetOrganisation(rtsId);
        if (rtsResponse.IsSuccessStatusCode)
        {
            auth.WithSelectedOrganisation(rtsId, rtsResponse.Content.Name);
        }
        else
        {
            // optional: decide whether this should hard-fail or just leave name null
            // I'd usually fail, because the page depends on the org context.
            return SponsorUserAuthorisationResult.Fail(controller.ServiceError(rtsResponse));
        }

        return auth;
    }
}