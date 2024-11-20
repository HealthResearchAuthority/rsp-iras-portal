using System.Collections.Immutable;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using static Rsp.Logging.Extensions.LoggingExtensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("[area]/[controller]/[action]", Name = "admin:[action]")]
[Authorize(Policy = "IsAdmin")]
[FeatureGate("Navigation.Admin")]
public class UsersController(IUserManagementService userManagementService, ILogger<UsersController> logger) : Controller
{
    private const string Error = nameof(Error);
    private const string UserView = nameof(UserView);
    private const string DeleteUserView = nameof(DeleteUserView);
    private const string UserRolesView = nameof(UserRolesView);

    /// <summary>
    /// Users home page, where it displays available users
    /// with the options to edit/delete or manage roles
    /// </summary>
    [Route("/admin/users", Name = "admin:users")]
    public async Task<IActionResult> Index()
    {
        logger.LogInformationHp("called");

        // get the users
        var response = await userManagementService.GetUsers();

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            var users = response.Content?.Users.Select(user => new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            }) ?? [];

            return View(users);
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View(Error, this.ProblemResult(response))
        };
    }

    /// <summary>
    /// Displays the empty UserView to create a user
    /// </summary>
    [HttpGet]
    public IActionResult CreateUser()
    {
        logger.LogInformationHp("called");

        ViewBag.Mode = "create";

        return View(UserView, new UserViewModel());
    }

    /// <summary>
    /// Creates or Edits a user in the database
    /// </summary>
    /// <param name="model"><see cref="UserViewModel"> that holds user data</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitUser(UserViewModel model)
    {
        logger.LogInformationHp("called");

        if (!ModelState.IsValid)
        {
            return View(UserView, model);
        }

        // Creates a user if in "create" mode i.e. model.Id is null
        // Updates the user if in "edit" mode i.e. model.Id has a value
        var response = string.IsNullOrWhiteSpace(model.Id) ?
                            await userManagementService.CreateUser(model.FirstName, model.LastName, model.Email) :
                            await userManagementService.UpdateUser(model.OriginalEmail!, model.FirstName, model.LastName, model.Email);

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
            _ => View(Error, this.ProblemResult(response))
        };
    }

    /// <summary>
    /// Displays the UserView in Edit mode
    /// </summary>
    /// <param name="userId">User Id</param>
    /// <param name="email">Email</param>
    [HttpGet]
    public async Task<IActionResult> EditUser(string userId, string email)
    {
        logger.LogInformationHp("called");

        ViewBag.Mode = "edit";

        // get user by userId and email
        var response = await userManagementService.GetUser(userId, email);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            var user = response.Content!.User;

            var model = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };

            return View(UserView, model);
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View(Error, this.ProblemResult(response))
        };
    }

    /// <summary>
    /// Displays the DeleteUserView for delete confirmation
    /// </summary>
    /// <param name="userId">User Id</param>
    /// <param name="email">Email</param>
    [HttpGet]
    public IActionResult DeleteUser(string userId, string email)
    {
        logger.LogInformationHp("called");

        var model = new UserViewModel
        {
            Id = userId,
            Email = email
        };

        return View(DeleteUserView, model);
    }

    /// <summary>
    /// Deletes the user from the database if confirmed
    /// </summary>
    /// <param name="model"><see cref="UserViewModel"/> that holds the user data</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(UserViewModel model)
    {
        logger.LogInformationHp("called");

        // deleting user
        var response = await userManagementService.DeleteUser(model.Id!, model.Email);

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
            _ => View(Error, this.ProblemResult(response))
        };
    }

    // POST: User/Create
    [HttpGet]
    public async Task<IActionResult> ManageRoles(string userId, string email)
    {
        logger.LogInformationHp("called");

        // get all the roles
        var getRolesResponse = await userManagementService.GetRoles();

        // empty roles list
        var roles = Enumerable.Empty<Role>();

        // build a list of roles
        if (getRolesResponse.IsSuccessStatusCode)
        {
            roles = getRolesResponse
                .Content?
                .Roles
                .Select(role => new Role(role.Id, role.Name)) ?? [];

            if (!roles.Any())
            {
                View(UserRolesView);
            }
        }
        else
        {
            return getRolesResponse.StatusCode switch
            {
                HttpStatusCode.Forbidden => Forbid(),
                _ => View(Error, this.ProblemResult(getRolesResponse))
            };
        }

        // get user
        var response = await userManagementService.GetUser(userId, email);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            var userRespone = response.Content!;

            var model = new UserRolesViewModel
            {
                UserId = userRespone.User.Id!,
                Email = email,
                UserRoles = roles.Select(role => new UserRoleViewModel
                {
                    Id = role.Id!,
                    Name = role.Name,
                    IsSelected = userRespone.Roles.Contains(role.Name, StringComparer.OrdinalIgnoreCase)
                }).ToList()
            };

            return View(UserRolesView, model);
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View(Error, this.ProblemResult(response))
        };
    }

    /// <summary>
    /// Updates user roles
    /// </summary>
    /// <param name="model"><see cref="UserRolesViewModel"/> thats holds the user roles data</param>
    [HttpPost]
    public async Task<IActionResult> UpdateRoles(UserRolesViewModel model)
    {
        logger.LogInformationHp("called");

        if (!ModelState.IsValid)
        {
            return View(UserRolesView, model);
        }

        // get roles to delete
        var rolesToDelete = model.UserRoles
            .Where(model => !model.IsSelected)
            .Select(model => model.Name)
            .ToList();

        // get roles to add
        var rolesToAdd = model.UserRoles
            .Where(model => model.IsSelected)
            .Select(model => model.Name)
            .ToList();

        // call update roles that will delete and add apprpriate roles
        var response = await userManagementService.UpdateRoles(model.Email, string.Join(',', rolesToDelete), string.Join(',', rolesToAdd));

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
            _ => View(Error, this.ProblemResult(response))
        };
    }
}