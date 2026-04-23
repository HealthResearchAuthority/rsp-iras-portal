using System.Security.Claims;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;

namespace Rsp.IrasPortal.Web.Helpers;

public static class MemberManagementHelper
{
    public static async Task<bool> UserHasAccess(ReviewBodyDto rec, ClaimsPrincipal user, IUserManagementService userService)
    {
        var isAdmin = user.IsInRole(Roles.SystemAdministrator);

        if (isAdmin)
        {
            // if user is an admin, allow access
            return true;
        }

        var userId = user?.FindFirst(CustomClaimTypes.UserId)?.Value;
        var userDetails = await userService.GetUser(userId, null);

        // if logged in user  cannot be found or is not Active
        // deny access
        if (!userDetails.IsSuccessStatusCode ||
            userDetails.Content == null ||
            userDetails.Content.User.Status != IrasUserStatus.Active)
        {
            return false;
        }

        var userCountry = userDetails.Content?.User?.Country?.Split(',');
        var recCountry = rec.Countries;

        var belongsToRec = userCountry?
            .Intersect(recCountry ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase)
            .Any() == true;

        // if user does not belong to the rec country then deny access
        if (!belongsToRec)
        {
            return false;
        }

        return true;
    }
}