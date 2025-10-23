using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Features.ProfileAndSettings.Controllers;

[Route("[controller]/[action]", Name = "profilesettings:[action]")]
[Authorize]
public class ProfileAndSettingsController(
    IUserManagementService userService,
    IValidator<UserViewModel> validator) : Controller
{
    private const string EditProfileView = nameof(EditProfileView);
    private const string Error = nameof(Error);

    [HttpGet("~/[controller]", Name = "profilesettings")]
    public async Task<IActionResult> Index()
    {
        // cehck if user model is in tempData
        var userModel = TempData["newUserProfile"];
        if (userModel is string json)
        {
            ViewBag.Mode = "complete";
            var viewModel = JsonSerializer.Deserialize<UserViewModel>(json);

            return View(viewModel);
        }
        else
        {
            ViewBag.Mode = "edit";
            var currentUserEmail = HttpContext?.User.FindFirstValue(ClaimTypes.Email);
            var userEntityResponse = await userService.GetUser(null, currentUserEmail);

            if (!userEntityResponse.IsSuccessStatusCode)
            {
                if (userEntityResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return RedirectToAction(nameof(EditProfile));
                }

                return this.ServiceError(userEntityResponse);
            }

            var viewModel = new UserViewModel(userEntityResponse.Content!);

            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile(UserViewModel? userModel = null)
    {
        // case for existing user editing their information
        var currentUserEmail = HttpContext?.User.FindFirstValue(ClaimTypes.Email);
        var userEntityResponse = await userService.GetUser(null, currentUserEmail);

        if (!userEntityResponse.IsSuccessStatusCode)
        {
            if (userEntityResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // user does not exist and they need to complete their profile
                var phone = HttpContext?.User.FindFirstValue(ClaimTypes.MobilePhone);
                var id = HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                ViewBag.Mode = "complete";

                var createViewModel = new UserViewModel()
                {
                    Email = currentUserEmail!,
                    OriginalEmail = currentUserEmail,
                    Telephone = phone,
                    IdentityProviderId = id,
                };

                return View(EditProfileView, createViewModel);
            }

            return this.ServiceError(userEntityResponse);
        }

        ViewBag.Mode = "edit";
        var viewModel = new UserViewModel(userEntityResponse.Content!);
        return View(EditProfileView, viewModel);
    }

    [HttpPost]
    [CmsContentAction(nameof(EditProfile))]
    public IActionResult EditNewUserProfile(UserViewModel userModel)
    {
        return View(EditProfileView, userModel);
    }

    [HttpPost]
    [CmsContentAction(nameof(EditProfile))]
    public async Task<IActionResult> SaveProfile(UserViewModel userModel)
    {
        var mode = (userModel.Id == null) ? "complete" : "edit";
        ViewBag.Mode = mode;

        var context = new ValidationContext<UserViewModel>(userModel);
        var validationResult = await validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(EditProfileView, userModel);
        }

        if (mode == "edit")
        {
            // save user changes
            var updateRequest = userModel.Adapt<UpdateUserRequest>();
            updateRequest.LastUpdated = DateTime.UtcNow;

            var updateUserRequest = await userService.UpdateUser(updateRequest);

            // if status is forbidden
            // return the appropriate response otherwise
            // return the generic error page
            if (!updateUserRequest.IsSuccessStatusCode)
            {
                return this.ServiceError(updateUserRequest);
            }

            // show notification banner for success message
            TempData[TempDataKeys.ShowNotificationBanner] = true;

            return RedirectToAction(nameof(Index));
        }
        else
        {
            // create new user
            var request = userModel.Adapt<CreateUserRequest>();
            request.Status = IrasUserStatus.Active;

            var createUserStatus = await userService.CreateUser(request);

            // user was created succesfully so let's assign them the 'applicant' role
            var assignRolesStatus = await userService.UpdateRoles(userModel.Email, null, "applicant");

            if (!createUserStatus.IsSuccessStatusCode || !assignRolesStatus.IsSuccessStatusCode)
            {
                return this.ServiceError(createUserStatus);
            }

            // show notification banner for success message
            TempData[TempDataKeys.ShowNotificationBanner] = true;

            //// redirect to homepage
            return RedirectToAction(nameof(ResearchAccountController.Home), "ResearchAccount");
        }
    }

    [HttpPost]
    [CmsContentAction(nameof(EditProfile))]
    public async Task<IActionResult> ConfirmProfileDetails(UserViewModel userModel)
    {
        ViewBag.Mode = "complete";
        var context = new ValidationContext<UserViewModel>(userModel);
        var validationResult = await validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(EditProfileView, userModel);
        }

        // serialise userModel and store as tempData
        TempData["newUserProfile"] = JsonSerializer.Serialize(userModel);

        return RedirectToAction(nameof(Index));
    }
}