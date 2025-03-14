using System.Collections.Immutable;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("[area]/[controller]/[action]", Name = "admin:[action]")]
[Authorize(Policy = "IsAdmin")]
[FeatureGate(Features.Admin)]
public class UsersController(IUserManagementService userManagementService) : Controller
{
    private const string Error = nameof(Error);
    private const string UserView = nameof(UserView);
    private const string ConfirmUser = nameof(ConfirmUser);
    private const string DeleteUserView = nameof(DeleteUserView);
    private const string UserRolesView = nameof(UserRolesView);
    private const string CreateUserSuccessMessage = nameof(CreateUserSuccessMessage);

    /// <summary>
    /// Users home page, where it displays available users
    /// with the options to edit/delete or manage roles
    /// </summary>
    [Route("/admin/users", Name = "admin:users")]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
    {
        // get the users
        var response = await userManagementService.GetUsers(pageNumber, pageSize);

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

            return View((users, response.Content.TotalCount, pageNumber, pageSize));
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
    public async Task<IActionResult> CreateUser()
    {
        ViewBag.Mode = "create";

        var model = new UserViewModel();
        var availableRoles = await userManagementService.GetRoles();

        if (availableRoles.IsSuccessStatusCode && availableRoles?.Content?.Roles != null)
        {
            model.AvailableUserRoles = availableRoles.Content.Roles.ToList();
        }

        return View(UserView, model);
    }

    /// <summary>
    /// Displays the edit UserView when creating a user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNewUser(UserViewModel model)
    {
        ViewBag.Mode = "create";

        // get all available roles to be presented on the FE
        var availableRoles = await userManagementService.GetRoles();

        if (availableRoles.IsSuccessStatusCode && availableRoles?.Content?.Roles != null)
        {
            model.AvailableUserRoles = availableRoles.Content.Roles.ToList();
        }

        return View(UserView, model);
    }

    /// <summary>
    /// Returns a ConfirUser view that displays user details for confirmation
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmUserSubmission(UserViewModel model)
    {
        ViewBag.Mode = string.IsNullOrEmpty(model.Id) ? "create" : "edit";
        if (!ModelState.IsValid)
        {
            // get all available roles to be presented on the FE if model is invalid
            var availableRoles = await userManagementService.GetRoles();

            if (availableRoles.IsSuccessStatusCode && availableRoles?.Content?.Roles != null)
            {
                model.AvailableUserRoles = availableRoles.Content.Roles.ToList();
            }

            return View(UserView, model);
        }

        return View(ConfirmUser, model);
    }

    /// <summary>
    /// Creates or Edits a user in the database
    /// </summary>
    /// <param name="model"><see cref="UserViewModel"> that holds user data</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitUser(UserViewModel model)
    {
        // convert country from array to comma seperated string to be stored in the database
        var country = model.Country != null ? string.Join(',', model.Country) : null;

        // Creates a user if in "create" mode i.e. model.Id is null
        // Updates the user if in "edit" mode i.e. model.Id has a value
        var submitUserResponse = string.IsNullOrWhiteSpace(model.Id) ?
                            await userManagementService.CreateUser(model.Title,
                            model.FirstName,
                            model.LastName,
                            model.Email,
                            model.JobTitle,
                            model.Organisation,
                            model.Telephone,
                            country,
                            IrasUserStatus.Active,
                            DateTime.UtcNow) :
                            await userManagementService.UpdateUser(model.OriginalEmail!,
                            model.Title,
                            model.FirstName,
                            model.LastName,
                            model.Email,
                            model.JobTitle,
                            model.Organisation,
                            model.Telephone,
                            country,
                            IrasUserStatus.Active,
                            DateTime.UtcNow);

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        if (!submitUserResponse.IsSuccessStatusCode)
        {
            return submitUserResponse.StatusCode switch
            {
                HttpStatusCode.Forbidden => Forbid(),
                _ => View(Error, this.ProblemResult(submitUserResponse))
            };
        }

        // assign role
        if (!string.IsNullOrEmpty(model.Role))
        {
            string? rolesToRemove = null;

            // if editing user, remove from existing roles before adding to newly selected role
            if (!string.IsNullOrEmpty(model.Id))
            {
                var existingUser = await userManagementService.GetUser(model.Id, model.Email);
                rolesToRemove = existingUser?.Content?.Roles != null ? string.Join(',', existingUser.Content.Roles) : null;
            }

            var roleResponse = await userManagementService.UpdateRoles(model.Email, rolesToRemove, model.Role);

            // if status is forbidden
            // return the appropriate response otherwise
            // return the generic error page
            if (!roleResponse.IsSuccessStatusCode)
            {
                return roleResponse.StatusCode switch
                {
                    HttpStatusCode.Forbidden => Forbid(),
                    _ => View(Error, this.ProblemResult(roleResponse))
                };
            }
        }

        if (string.IsNullOrWhiteSpace(model.Id))
        {
            // for successful creation of new user, present a success message view
            return View(CreateUserSuccessMessage, model);
        }
        else
        {
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Displays the UserView in Edit mode
    /// </summary>
    /// <param name="userId">User Id</param>
    /// <param name="email">Email</param>
    [HttpGet]
    public async Task<IActionResult> EditUser(string userId, string email)
    {
        ViewBag.Mode = "edit";

        // get user by userId and email
        var response = await userManagementService.GetUser(userId, email);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            var user = response.Content!.User;
            var roles = response.Content!.Roles;

            var model = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Telephone = user.Telephone,
                Country = !string.IsNullOrEmpty(user.Country) ? user.Country.Split(',') : null,
                Title = user.Title,
                JobTitle = user.JobTitle,
                Organisation = user.Organisation,
                Role = roles != null ? roles.FirstOrDefault() : null,
                LastUpdated = user.lastUpdated
            };

            var availableRoles = await userManagementService.GetRoles();

            if (availableRoles.IsSuccessStatusCode && availableRoles?.Content?.Roles != null)
            {
                model.AvailableUserRoles = availableRoles.Content.Roles.ToList();
            }

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
        // get all the roles
        var getRolesResponse = await userManagementService.GetRoles();

        // empty roles list
        IEnumerable<Role> roles;

        // build a list of roles
        if (getRolesResponse.IsSuccessStatusCode)
        {
            roles = getRolesResponse
                .Content?
                .Roles
                .Select(role => new Role(role.Id, role.Name)) ?? [];

            if (!roles.Any())
            {
                return View(UserRolesView);
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