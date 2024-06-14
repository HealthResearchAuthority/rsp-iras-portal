using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("[area]/[controller]/[action]", Name = "admin:[action]")]
[Authorize(Policy = "IsAdmin")]
public class HomeController(IUserManagementService userManagementService, ILogger<HomeController> logger) : Controller
{
    [Route("/admin", Name = "admin:home")]
    public async Task<IActionResult> Index()
    {
        logger.LogMethodStarted(LogLevel.Information);

        // get the users
        var getUsersResponse = userManagementService.GetUsers();
        var getRolesResponse = userManagementService.GetRoles();

        await Task.WhenAll(getUsersResponse, getRolesResponse);

        var usersResult = getUsersResponse.Result;
        var rolesResult = getRolesResponse.Result;

        // return the view if successfull
        if (usersResult.IsSuccessStatusCode && rolesResult.IsSuccessStatusCode)
        {
            var users = usersResult.Content!.Users.Count();
            var roles = rolesResult.Content!.Roles.Count();

            (int UserCount, int RoleCount) data = (users, roles);

            return View(data);
        }

        if (usersResult.StatusCode == HttpStatusCode.Forbidden ||
            rolesResult.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }

        return View("Error", new ProblemDetails
        {
            Title = "An unexpected error has occured, please try again",
            Detail = "Error getting the count of users and roles",
            Instance = Request.Path,
            Status = StatusCodes.Status500InternalServerError
        });
    }
}