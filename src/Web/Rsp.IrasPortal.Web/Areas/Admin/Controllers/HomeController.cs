using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Controllers;

[Authorize(Policy = Workspaces.SystemAdministration)]
[Area("Admin")]
[Route("[area]/[controller]/[action]", Name = "admin:[action]")]
[FeatureGate(FeatureFlags.Admin)]
public class HomeController(IUserManagementService userManagementService) : Controller
{
    [Route("/admin", Name = "admin:home")]
    public async Task<IActionResult> Index()
    {
        // get the users
        var getUsersResponse = userManagementService.GetUsers();
        var getRolesResponse = userManagementService.GetRoles();

        await Task.WhenAll(getUsersResponse, getRolesResponse);

        var usersResult = getUsersResponse.Result;
        var rolesResult = getRolesResponse.Result;

        // return the view if successfull
        if (usersResult.IsSuccessStatusCode && rolesResult.IsSuccessStatusCode)
        {
            var users = usersResult.Content!.TotalCount;
            var roles = rolesResult.Content!.TotalCount;

            (int UserCount, int RoleCount) data = (users, roles);

            return View(data);
        }

        if (usersResult.StatusCode == HttpStatusCode.Forbidden ||
            rolesResult.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }

        var serviceResponse = new ServiceResponse()
            .WithError("Error getting the count of users and roles")
            .WithReason("An unexpected error has occured, please try again");

        return this.ServiceError(serviceResponse);
    }
}