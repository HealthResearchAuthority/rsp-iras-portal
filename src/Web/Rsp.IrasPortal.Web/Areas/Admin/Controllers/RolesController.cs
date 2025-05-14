using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("[area]/[controller]/[action]", Name = "admin:[action]")]
[Authorize(Policy = "IsAdmin")]
[FeatureGate(Features.Admin)]
public class RolesController(IUserManagementService userManagementService) : Controller
{
    /// <summary>
    /// Roles home page, where it displays available roles
    /// with the options to edit or delete role
    /// </summary>
    [Route("/admin/roles", Name = "admin:roles")]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
    {
        // get all roles
        var response = await userManagementService.GetRoles(pageNumber, pageSize);

        // return the view with the RoleViewModel if successfull
        if (response.IsSuccessStatusCode)
        {
            var roles = response.Content?.Roles.Select(role => new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name
            }) ?? [];

            var paginationModel = new PaginationViewModel(pageNumber, pageSize, response.Content?.TotalCount ?? 0)
            {
                RouteName = "admin:roles",
            };

            return View((roles, paginationModel));
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View("Error", this.ProblemResult(response))
        };
    }

    /// <summary>
    /// Displays the empty RoleView to create a role
    /// </summary>
    [HttpGet]
    public IActionResult CreateRole()
    {
        ViewBag.Mode = "create";

        return View("RoleView", new RoleViewModel());
    }

    /// <summary>
    /// Creates or Edits a role in the database
    /// </summary>
    /// <param name="model"><see cref="RoleViewModel"> that holds role data</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRole(RoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("RoleView", model);
        }

        // Creates a role if in "create" mode i.e. model.Id is null
        // Updates the role if in "edit" mode i.e. model.Id has a value
        var response = string.IsNullOrWhiteSpace(model.Id) ?
                            await userManagementService.CreateRole(model.Name) :
                            await userManagementService.UpdateRole(model.OriginalName!, model.Name);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View("Error", this.ProblemResult(response))
        };
    }

    /// <summary>
    /// Displays the RoleView in Edit mode
    /// </summary>
    /// <param name="roleId">role Id</param>
    /// <param name="roleName">role Name</param>
    [HttpGet]
    public IActionResult EditRole(string roleId, string roleName)
    {
        ViewBag.Mode = "edit";

        var model = new RoleViewModel
        {
            Id = roleId,
            OriginalName = roleName,
            Name = roleName
        };

        return View("RoleView", model);
    }

    /// <summary>
    /// Displays the DeleteRoleView for delete confirmation
    /// </summary>
    /// <param name="roleId">role Id</param>
    /// <param name="roleName">role Name</param>
    [HttpGet]
    public IActionResult DeleteRole(string roleId, string roleName)
    {
        var model = new RoleViewModel
        {
            Id = roleId,
            Name = roleName
        };

        return View("DeleteRoleView", model);
    }

    /// <summary>
    /// Deletes the role from the database if confirmed
    /// </summary>
    /// <param name="model"><see cref="RoleViewModel"/> that holds the role data</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoleConfirmed(RoleViewModel model)
    {
        // deleting user role
        var response = await userManagementService.DeleteRole(model.Name);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View("Error", this.ProblemResult(response))
        };
    }
}