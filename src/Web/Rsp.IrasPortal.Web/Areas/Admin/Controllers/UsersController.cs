using System.Collections.Immutable;
using System.Net;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("[area]/[controller]/[action]", Name = "admin:[action]")]
[Authorize(Policy = "IsAdmin")]
[FeatureGate(Features.Admin)]
public class UsersController(IUserManagementService userManagementService, IValidator<UserViewModel> validator) : Controller
{
    private const string Error = nameof(Error);
    private const string EditUserView = nameof(EditUserView);
    private const string ConfirmUser = nameof(ConfirmUser);
    private const string ViewUserView = nameof(ViewUserView);
    private const string DeleteUserView = nameof(DeleteUserView);
    private const string UserRolesView = nameof(UserRolesView);
    private const string CreateUserSuccessMessage = nameof(CreateUserSuccessMessage);
    private const string DisableUserSuccessMessage = nameof(DisableUserSuccessMessage);
    private const string ConfirmDisableUser = nameof(ConfirmDisableUser);
    private const string EnableUserSuccessMessage = nameof(EnableUserSuccessMessage);
    private const string ConfirmEnableUser = nameof(ConfirmEnableUser);

    private const string EditMode = "edit";
    private const string CreateMode = "create";

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
                Email = user.Email,
                Status = user.Status,
                LastLogin = user.LastLogin
            }) ?? [];

            var paginationModel = new PaginationViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                RouteName = "admin:users",
                TotalCount = response.Content?.TotalCount ?? 0
            };

            return View((users, paginationModel));
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
        ViewBag.Mode = CreateMode;

        var model = new UserViewModel();
        model.AvailableUserRoles = await GetAlluserRoles();

        return View(EditUserView, model);
    }

    /// <summary>
    /// Displays the edit UserView when creating a user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNewUser(UserViewModel model)
    {
        ViewBag.Mode = CreateMode;

        // get all available roles to be presented on the FE
        model.AvailableUserRoles = await GetAlluserRoles();

        return View(EditUserView, model);
    }

    /// <summary>
    /// Returns a ConfirUser view that displays user details for confirmation
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmUserSubmission(UserViewModel model)
    {
        ViewBag.Mode = string.IsNullOrEmpty(model.Id) ? CreateMode : EditMode;

        var context = new ValidationContext<UserViewModel>(model);
        var validationResult = await validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            // get all available roles to be presented on the FE if model is invalid
            model.AvailableUserRoles = await GetAlluserRoles();

            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(EditUserView, model);
        }

        return View(ConfirmUser, model);
    }

    [HttpGet]
    public async Task<IActionResult> ViewUser(string userId, string email)
    {
        // get user by userId and email
        var response = await userManagementService.GetUser(userId, email);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            var model = new UserViewModel(response.Content!);

            return View(ViewUserView, model);
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
    /// Creates or Edits a user in the database
    /// </summary>
    /// <param name="model"><see cref="UserViewModel"> that holds user data</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitUser(UserViewModel model)
    {
        var mode = string.IsNullOrEmpty(model.Id) ? CreateMode : EditMode;
        ViewBag.Mode = mode;

        var context = new ValidationContext<UserViewModel>(model);
        var validationResult = await validator.ValidateAsync(context);

        // check if modelstate is valid if in edit mode
        if (mode == EditMode && !validationResult.IsValid)
        {
            // get all available roles to be presented on the FE if model is invalid
            model.AvailableUserRoles = await GetAlluserRoles();

            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(EditUserView, model);
        }

        // Creates a user if in "create" mode i.e. model.Id is null
        // Updates the user if in "edit" mode i.e. model.Id has a value
        var submitUserResponse = await CreateOrUpdateUser(model, mode);

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
            var roleResponse = await UpdateUserRoles(model);

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

        return mode switch
        {
            CreateMode => View(CreateUserSuccessMessage, model),
            _ => RedirectToAction(nameof(ViewUser), new { userId = model.Id, email = model.Email })
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
        ViewBag.Mode = EditMode;

        // get user by userId and email
        var response = await userManagementService.GetUser(userId, email);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            var model = new UserViewModel(response.Content!);
            model.AvailableUserRoles = await GetAlluserRoles();

            return View(EditUserView, model);
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
    public async Task<IActionResult> DisableUser(string userId, string email)
    {
        var response = await userManagementService.GetUser(userId, email);

        if (response.IsSuccessStatusCode)
        {
            var model = new UserViewModel(response.Content!);

            return View(ConfirmDisableUser, model);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableUser(UserViewModel model)
    {
        var userResponse = await userManagementService.GetUser(model.Id, model.Email);
        if (userResponse.IsSuccessStatusCode)
        {
            var updateModel = new UserViewModel(userResponse.Content!);

            var updateUserRequest = updateModel.Adapt<UpdateUserRequest>();
            updateUserRequest.LastUpdated = DateTime.UtcNow;
            updateUserRequest.Status = IrasUserStatus.Disabled;

            var updateDisabledUser = await userManagementService.UpdateUser(updateUserRequest);

            if (updateDisabledUser.IsSuccessStatusCode)
            {
                return View(DisableUserSuccessMessage, updateModel);
            }
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return userResponse.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View(Error, this.ProblemResult(userResponse))
        };
    }

    [HttpGet]
    public async Task<IActionResult> EnableUser(string userId, string email)
    {
        var response = await userManagementService.GetUser(userId, email);

        if (response.IsSuccessStatusCode)
        {
            var model = new UserViewModel(response.Content!);

            return View(ConfirmEnableUser, model);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableUser(UserViewModel model)
    {
        var userResponse = await userManagementService.GetUser(model.Id, model.Email);
        if (userResponse.IsSuccessStatusCode)
        {
            var updateModel = new UserViewModel(userResponse.Content!);

            var updateUserRequest = updateModel.Adapt<UpdateUserRequest>();
            updateUserRequest.LastUpdated = DateTime.UtcNow;
            updateUserRequest.Status = IrasUserStatus.Active;

            var updateDisabledUser = await userManagementService.UpdateUser(updateUserRequest);

            if (updateDisabledUser.IsSuccessStatusCode)
            {
                return View(EnableUserSuccessMessage, updateModel);
            }
        }

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        return userResponse.StatusCode switch
        {
            HttpStatusCode.Forbidden => Forbid(),
            _ => View(Error, this.ProblemResult(userResponse))
        };
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

    private async Task<IList<Role>> GetAlluserRoles()
    {
        var availableRoles = await userManagementService.GetRoles();

        if (availableRoles.IsSuccessStatusCode && availableRoles.Content?.Roles != null)
        {
            return availableRoles.Content.Roles.ToList();
        }

        return new List<Role>();
    }

    private async Task<ServiceResponse> CreateOrUpdateUser(UserViewModel model, string mode)
    {
        if (mode == CreateMode)
        {
            var createRequest = model.Adapt<CreateUserRequest>();
            createRequest.Status = IrasUserStatus.Active;

            return await userManagementService.CreateUser(createRequest);
        }

        var updateRequest = model.Adapt<UpdateUserRequest>();
        updateRequest.LastUpdated = DateTime.UtcNow;

        return await userManagementService.UpdateUser(updateRequest);
    }

    private async Task<ServiceResponse> UpdateUserRoles(UserViewModel model)
    {
        string? rolesToRemove = null;

        // if editing user, remove from existing roles before adding to newly selected role
        if (!string.IsNullOrEmpty(model.Id))
        {
            var existingUser = await userManagementService.GetUser(model.Id, model.Email);
            rolesToRemove = existingUser?.Content?.Roles != null ? string.Join(',', existingUser.Content.Roles) : null;
        }

        return await userManagementService.UpdateRoles(model.Email, rolesToRemove, model.Role!);
    }
}