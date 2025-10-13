using System.Net;
using System.Security.Claims;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Mode = "edit";
        var currentUserEmail = HttpContext?.User.FindFirstValue(ClaimTypes.Email);
        var userEntityResponse = await userService.GetUser(null, currentUserEmail);

        if (!userEntityResponse.IsSuccessStatusCode && userEntityResponse.Content == null)
        {
            return this.ServiceError(userEntityResponse);
        }

        var viewModel = new UserViewModel(userEntityResponse.Content!);

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        ViewBag.Mode = "edit";
        var currentUserEmail = HttpContext?.User.FindFirstValue(ClaimTypes.Email);
        var userEntityResponse = await userService.GetUser(null, currentUserEmail);

        if (!userEntityResponse.IsSuccessStatusCode && userEntityResponse.Content == null)
        {
            return this.ServiceError(userEntityResponse);
        }

        var viewModel = new UserViewModel(userEntityResponse.Content!);
        return View(EditProfileView, viewModel);
    }

    [HttpPost]
    [CmsContentAction(nameof(EditProfile))]
    public async Task<IActionResult> SaveProfile(UserViewModel userModel)
    {
        ViewBag.Mode = (userModel.Id == null) ? "create" : "edit";

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

        // save user changes

        var updateRequest = userModel.Adapt<UpdateUserRequest>();
        updateRequest.LastUpdated = DateTime.UtcNow;

        var updateUserRequest = await userService.UpdateUser(updateRequest);

        // if status is forbidden
        // return the appropriate response otherwise
        // return the generic error page
        if (!updateUserRequest.IsSuccessStatusCode)
        {
            return updateUserRequest.StatusCode switch
            {
                HttpStatusCode.Forbidden => Forbid(),
                _ => View(Error, this.ProblemResult(updateUserRequest))
            };
        }

        // show notification banner for success message
        TempData[TempDataKeys.ShowNotificationBanner] = true;

        return RedirectToAction(nameof(Index));
    }
}