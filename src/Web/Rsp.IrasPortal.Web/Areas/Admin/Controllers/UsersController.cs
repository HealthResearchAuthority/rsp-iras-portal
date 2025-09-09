using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("[area]/[controller]/[action]", Name = "admin:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
[FeatureGate(Features.Admin)]
public class UsersController(
    IUserManagementService userManagementService,
    IValidator<UserViewModel> validator,
    IValidator<UserSearchModel> searchValidator,
    IReviewBodyService reviewBodyService) : Controller
{
    private const string Error = nameof(Error);
    private const string EditUserView = nameof(EditUserView);
    private const string ConfirmUser = nameof(ConfirmUser);
    private const string ViewUserView = nameof(ViewUserView);
    private const string UserRolesView = nameof(UserRolesView);
    private const string CreateUserSuccessMessage = nameof(CreateUserSuccessMessage);
    private const string DisableUserSuccessMessage = nameof(DisableUserSuccessMessage);
    private const string ConfirmDisableUser = nameof(ConfirmDisableUser);
    private const string EnableUserSuccessMessage = nameof(EnableUserSuccessMessage);
    private const string ConfirmEnableUser = nameof(ConfirmEnableUser);
    private const string TeamManagerRole = "team_manager";
    private const string StudyWideReviewerRole = "study-wide_reviewer";
    private const string WorkflowCoordinatorRole = "workflow_co-ordinator";

    private const string EditMode = "edit";
    private const string CreateMode = "create";

    /// <summary>
    /// Users home page, where it displays available users
    /// with the options to edit/delete or manage roles
    /// </summary>
    [Route("/admin/users", Name = "admin:users")]
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index(int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(UserViewModel.GivenName),
        string? sortDirection = SortDirections.Ascending,
        [FromForm] UserSearchViewModel? model = null,
        [FromQuery] bool fromPagination = false)
    {
        if (!fromPagination)
        {
            // RESET ON SEARCH AND REMOVE FILTERS
            pageNumber = 1;
            pageSize = 20;
        }

        model ??= new UserSearchViewModel();
        model.Search ??= new UserSearchModel();

        // Always attempt to restore from session if nothing is currently set
        var savedSearch = HttpContext.Session.GetString(SessionKeys.UsersSearch);
        if (!string.IsNullOrWhiteSpace(savedSearch))
        {
            model.Search = JsonSerializer.Deserialize<UserSearchModel>(savedSearch);
        }

        var request = new SearchUserRequest()
        {
            SearchQuery = model.Search.SearchQuery,
            Country = model.Search.Country,
            Status = model.Search.Status,
            FromDate = model.Search.FromDate,
            ToDate = model.Search.ToDate?.AddDays(1).AddTicks(-1), // Inclusive end date
            Role = model.Search.UserRoles.Where(x => x.IsSelected).Select(x => x.Id).ToList(),
        };

        if (model.Search.ReviewBodies.Any(x => x.IsSelected))
        {
            var selectedReviewBodyIds = model.Search.ReviewBodies
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToList();

            var userReviewBodies = await GetUserReviewBodiesByIds(selectedReviewBodyIds);

            var allowedUserIds = userReviewBodies
                .Select(urb => urb.UserId.ToString().ToUpperInvariant()) // adjust if property name differs
                .ToHashSet();

            request.UserIds = allowedUserIds.ToList();
        }

        // get the users
        var response = await userManagementService.GetUsers(request, pageNumber, pageSize, sortField, sortDirection);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            var users = response.Content?.Users.Select(user => new UserViewModel(user)) ?? [];
            var availableRoles = await GetAlluserRoles();
            var activeReviewBodies = await GetActiveReviewBodies();

            var paginationModel = new PaginationViewModel(pageNumber, pageSize, response.Content?.TotalCount ?? 0)
            {
                RouteName = "admin:users",
                SortField = sortField,
                SortDirection = sortDirection
            };

            var reviewBodySearchViewModel = new UserSearchViewModel()
            {
                Pagination = paginationModel,
                Users = users,
                Search = model.Search,
            };

            if (model.Search.UserRoles.Count == 0)
            {
                reviewBodySearchViewModel.Search.UserRoles = availableRoles.Select(role => new UserRoleViewModel
                {
                    Id = role.Id,
                    Name = role.Name
                }).ToList();
            }
            else
            {
                reviewBodySearchViewModel.Search.UserRoles = model.Search.UserRoles;
            }

            if (model.Search.ReviewBodies.Count == 0)
            {
                reviewBodySearchViewModel.Search.ReviewBodies = activeReviewBodies.Select(role =>
                    new UserReviewBodyViewModel()
                    {
                        Id = role.Id,
                        RegulatoryBodyName = role.RegulatoryBodyName,
                    }).ToList();
            }
            else
            {
                reviewBodySearchViewModel.Search.ReviewBodies = model.Search.ReviewBodies;
            }

            // Save the current search filters to the session
            HttpContext.Session.SetString(SessionKeys.UsersSearch, JsonSerializer.Serialize(reviewBodySearchViewModel.Search));

            return View("Index", reviewBodySearchViewModel);
        }

        // return error page as api wasn't successful
        return this.ServiceError(response);
    }

    /// <summary>
    /// Displays the empty UserView to create a user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        ViewBag.Mode = CreateMode;

        var availableRoles = await GetAlluserRoles();
        var activeReviewBodies = await GetActiveReviewBodies();

        var model = new UserViewModel
        {
            UserRoles = availableRoles.Select(role => new UserRoleViewModel
            {
                Id = role.Id,
                Name = role.Name
            }).ToList(),
            ReviewBodies = activeReviewBodies.Select(rb => new UserReviewBodyViewModel()
            {
                Id = rb.Id,
                RegulatoryBodyName = rb.RegulatoryBodyName
            }).ToList()
        };

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
        var availableRoles = await GetAlluserRoles();
        var activeReviewBodies = await GetActiveReviewBodies();

        if (model.UserRoles.Count == 0)
        {
            model.UserRoles = availableRoles.Select(role => new UserRoleViewModel
            {
                Id = role.Id,
                Name = role.Name
            }).ToList();
        }

        if (model.ReviewBodies.Count == 0)
        {
            model.ReviewBodies = activeReviewBodies.Select(rb => new UserReviewBodyViewModel()
            {
                Id = rb.Id,
                RegulatoryBodyName = rb.RegulatoryBodyName
            }).ToList();
        }

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

        ValidateUserViewModel(model);

        var context = new ValidationContext<UserViewModel>(model);
        var validationResult = await validator.ValidateAsync(context);

        var response = await userManagementService.GetUser(null, model.Email);

        var emailExists = response.IsSuccessStatusCode && response.Content != null;

        if (!validationResult.IsValid || emailExists)
        {
            if (emailExists)
            {
                validationResult.Errors.Add(new ValidationFailure("Email", "This user already exists", null));
            }

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

    private static void ValidateUserViewModel(UserViewModel model)
    {
        if (model.UserRoles.Any(r =>
                r.Name.Equals(TeamManagerRole, StringComparison.OrdinalIgnoreCase) && !r.IsSelected))
        {
            model.Country = [];
        }

        // Is either role selected?
        var hasEitherRoleSelected = model.UserRoles.Any(r =>
            (r.Name.Equals(StudyWideReviewerRole, StringComparison.OrdinalIgnoreCase) ||
             r.Name.Equals(WorkflowCoordinatorRole, StringComparison.OrdinalIgnoreCase))
            && r.IsSelected
        );

        // If neither role is selected and any review bodies are selected, clear them
        if (!hasEitherRoleSelected && (model.ReviewBodies?.Any(rb => rb.IsSelected) == true))
        {
            foreach (var rb in model.ReviewBodies)
            {
                rb.IsSelected = false;
            }
        }
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

            // GET THE REVIEW BODIES HERE
            model.ReviewBodies = await GetUserReviewBodies(Guid.Parse(model.Id));

            model.UserRoles = model.UserRoles
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

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

        ValidateUserViewModel(model);

        var context = new ValidationContext<UserViewModel>(model);
        var validationResult = await validator.ValidateAsync(context);

        // check if modelstate is valid if in edit mode
        if (mode == EditMode && !validationResult.IsValid)
        {
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
        if (model.UserRoles.Any())
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

        // assign to selected review bodies here
        await AddUpdateUsersToReviewBodies(model, model.ReviewBodies?.ToList());

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
            var availableRoles = await GetAlluserRoles();
            var activeReviewBodies = await GetActiveReviewBodies();

            foreach (var role in availableRoles)
            {
                // check if the user has the role
                // if the user has the role, set it to selected
                if (!model.UserRoles.Any(x => x.Name == role.Name))
                {
                    // user is not in role so add it
                    model.UserRoles.Add(new UserRoleViewModel
                    {
                        IsSelected = false,
                        Id = role.Id,
                        Name = role.Name
                    });
                }
            }

            model.UserRoles = model.UserRoles
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            model.ReviewBodies = await GetUserReviewBodies(Guid.Parse(model.Id));

            return View(EditUserView, model);
        }

        // return error page as api wasn't successful
        return this.ServiceError(response);
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

        // return error page as api wasn't successful
        return this.ServiceError(response);
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

        // return error page as api wasn't successful
        return this.ServiceError(userResponse);
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

        // return error page as api wasn't successful
        return this.ServiceError(response);
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

        // return error page as api wasn't successful
        return this.ServiceError(userResponse);
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

        // return error page as api wasn't successful
        return this.ServiceError(response);
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
        var response = await userManagementService.UpdateRoles(model.Email, string.Join(',', rolesToDelete),
            string.Join(',', rolesToAdd));

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
    /// Displays the user audit trail
    /// </summary>
    [HttpGet]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter",
        Justification = "Email is needed for backlink in view")]
    public async Task<IActionResult> UserAuditTrail(string userId, string email, string name)
    {
        var userAuditTrail = await userManagementService.GetUserAuditTrail(userId);

        var model = new UserAuditTrailViewModel()
        {
            Name = name,
            Items = [],
        };

        if (userAuditTrail.IsSuccessStatusCode)
        {
            model = userAuditTrail.Content.Adapt<UserAuditTrailViewModel>();
        }

        return View(model);
    }

    [Route("/admin/applyfilters", Name = "admin:applyfilters")]
    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> ApplyFilters(
        UserSearchViewModel model,
        string? sortField = nameof(UserViewModel.GivenName),
        string? sortDirection = SortDirections.Ascending,
        [FromQuery] bool fromPagination = false)
    {
        var validationResult = await searchValidator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(Index), model); // Return with validation errors
        }

        // Save applied filters to session
        HttpContext.Session.SetString(SessionKeys.UsersSearch, JsonSerializer.Serialize(model.Search));

        // Call Index with matching parameter set
        return await Index(
            1, // pageNumber
            20, // pageSize
            sortField,
            sortDirection,
            model,
            fromPagination);
    }

    [HttpGet]
    [Route("/admin/clearfilters", Name = "admin:clearfilters")]
    public IActionResult ClearFilters([FromQuery] string? searchQuery = null)
    {
        var cleanedSearch = new UserSearchModel
        {
            SearchQuery = searchQuery
        };

        // Clear any saved filters from session
        HttpContext.Session.Remove(SessionKeys.UsersSearch);

        // Save the current search filters to the session
        HttpContext.Session.SetString(SessionKeys.UsersSearch, JsonSerializer.Serialize(cleanedSearch));

        return RedirectToRoute("admin:users", new
        {
            pageNumber = 1,
            pageSize = 20,
            fromPagination = false
        });
    }

    [HttpGet]
    [Route("/admin/users/removefilter", Name = "admin:removefilter")]
    public IActionResult RemoveFilter(string key, string? value, [FromQuery] string? model = null)
    {
        var viewModel = new UserSearchViewModel();

        if (!string.IsNullOrWhiteSpace(model))
        {
            viewModel.Search = JsonSerializer.Deserialize<UserSearchModel>(model);
        }
        else
        {
            viewModel.Search = new UserSearchModel();
        }

        switch (key)
        {
            case UsersSearch.CountryKey:
                if (!string.IsNullOrEmpty(value) && viewModel.Search.Country != null)
                {
                    viewModel.Search.Country = viewModel.Search.Country
                        .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                break;

            case UsersSearch.FromDateKey:
                viewModel.Search.FromDay = viewModel.Search.FromMonth = viewModel.Search.FromYear = null;
                break;

            case UsersSearch.ToDateKey:
                viewModel.Search.ToDay = viewModel.Search.ToMonth = viewModel.Search.ToYear = null;
                break;

            case UsersSearch.StatusKey:
                viewModel.Search.Status = null;
                break;
            // NEW: turn off selected Review Bodies
            case UsersSearch.ReviewBodyKey:
                {
                    // if value is empty -> clear all selections
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        foreach (var rb in viewModel.Search.ReviewBodies) rb.IsSelected = false;
                        break;
                    }

                    var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var token in tokens)
                    {
                        if (Guid.TryParse(token, out var id))
                        {
                            foreach (var rb in viewModel.Search.ReviewBodies.Where(x => x.Id == id))
                                rb.IsSelected = false;
                        }
                        else
                        {
                            foreach (var rb in viewModel.Search.ReviewBodies.Where(x =>
                                         string.Equals(x.RegulatoryBodyName, token, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(x.DisplayName, token, StringComparison.OrdinalIgnoreCase)))
                                rb.IsSelected = false;
                        }
                    }

                    break;
                }

            // NEW: turn off selected Roles
            case UsersSearch.RoleKey:
                {
                    foreach (var r in viewModel.Search.UserRoles.Where(x => x.DisplayName == value))
                    {
                        r.IsSelected = false;
                    }
                }
                break;
        }

        // Save applied filters to session
        HttpContext.Session.SetString(SessionKeys.UsersSearch, JsonSerializer.Serialize(viewModel.Search));

        // Redirect to ViewReviewBodies with query parameters
        return RedirectToRoute("admin:users", new
        {
            pageNumber = 1,
            pageSize = 20,
            fromPagination = true
        });
    }

    private async Task<IList<Role>> GetAlluserRoles()
    {
        var availableRoles = await userManagementService.GetRoles();

        if (availableRoles.IsSuccessStatusCode && availableRoles.Content?.Roles != null)
        {
            return availableRoles.Content.Roles.OrderBy(x => x.Name).ToList();
        }

        return [];
    }

    private async Task<IList<ReviewBodyDto>> GetActiveReviewBodies()
    {
        var activeReviewBodies = await reviewBodyService.GetAllActiveReviewBodies();

        if (activeReviewBodies.IsSuccessStatusCode && activeReviewBodies.Content?.ReviewBodies != null)
        {
            return activeReviewBodies.Content.ReviewBodies.ToList();
        }

        return [];
    }

    private async Task<IList<UserReviewBodyViewModel>> GetUserReviewBodies(Guid id)
    {
        var activeReviewBodies = await GetActiveReviewBodies(); // IList<ReviewBodyDto>
        var userReviewBodiesResponse = await reviewBodyService.GetUserReviewBodies(id);

        if (userReviewBodiesResponse is { IsSuccessStatusCode: true, Content: not null })
        {
            var selectedIds = userReviewBodiesResponse.Content.Select(x => x.Id).ToHashSet();

            // (Re)build the model list in the required shape

            return activeReviewBodies.Select(reviewBody =>
                new UserReviewBodyViewModel
                {
                    IsSelected = selectedIds.Contains(reviewBody.Id),
                    Id = reviewBody.Id,
                    RegulatoryBodyName = reviewBody.RegulatoryBodyName
                }).ToList();
        }

        return [];
    }

    private async Task<IList<ReviewBodyUserDto>> GetUserReviewBodiesByIds(List<Guid> ids)
    {
        var userReviewBodiesResponse = await reviewBodyService.GetUserReviewBodiesByReviewBodyIds(ids);
        return userReviewBodiesResponse is { IsSuccessStatusCode: true, Content: not null }
            ? userReviewBodiesResponse.Content
            : [];
    }

    private async Task AddUpdateUsersToReviewBodies(UserViewModel model,
        List<UserReviewBodyViewModel>? userReviewBodyViewModels)
    {
        if (userReviewBodyViewModels != null)
        {
            var userId = Guid.Empty;

            if (!string.IsNullOrEmpty(model.Id))
            {
                userId = Guid.Parse(model.Id);
            }
            else
            {
                // Get user as it's a new user and don't need to remove review bodies user
                var userResponse = await userManagementService.GetUser(null, model.Email);

                if (userResponse.IsSuccessStatusCode && userResponse.Content?.User != null)
                {
                    userId = Guid.Parse(userResponse.Content?.User?.Id ?? string.Empty);
                }
            }

            // existingSelections: previously assigned review bodies for the user
            var existingSelections = await GetUserReviewBodies(userId);

            // Build fast lookup sets
            var existingIds = existingSelections
                .Where(x => x.IsSelected)
                .Select(x => x.Id) // FK to ReviewBody
                .ToHashSet();

            var currentlySelectedIds = userReviewBodyViewModels
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToHashSet();

            // Compute deltas
            var toRemove = existingIds.Except(currentlySelectedIds); // previously selected, now unselected -> remove
            var toAdd = currentlySelectedIds.Except(existingIds); // newly selected, not already assigned -> add

            // Remove only those that were previously selected but are no longer selected
            foreach (var rbId in toRemove)
            {
                await reviewBodyService.RemoveUserFromReviewBody(rbId, userId);
            }

            // Add only those newly selected that aren't already in existing selections
            foreach (var rbId in toAdd)
            {
                await reviewBodyService.AddUserToReviewBody(new ReviewBodyUserDto
                {
                    DateAdded = DateTime.UtcNow,
                    UserId = userId,
                    Email = model.Email,
                    Id = rbId
                });
            }
        }
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

        // If editing an existing user, remove them from current roles before assigning new ones
        if (!string.IsNullOrEmpty(model.Id))
        {
            var existingUser = await userManagementService.GetUser(model.Id, model.Email);
            rolesToRemove = existingUser?.Content?.Roles != null ? string.Join(',', existingUser.Content.Roles) : null;
        }

        // Collect all selected roles
        var selectedRoles = model
            .UserRoles!
            .Where(ur => ur.IsSelected)
            .Select(ur => ur.Name);

        // Convert to a comma-separated string
        string userRoles = string.Join(",", selectedRoles);

        return await userManagementService.UpdateRoles(model.Email, rolesToRemove, userRoles);
    }
}