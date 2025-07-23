using System.Data;
using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "rbc:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
public class ReviewBodyController(
    IReviewBodyService reviewBodyService,
    IUserManagementService userService,
    IValidator<AddUpdateReviewBodyModel> validator)
    : Controller
{
    private const string Error = nameof(Error);
    private const string CreateUpdateReviewBodyView = nameof(CreateReviewBody);
    private const string ViewReviewBodyView = nameof(ViewReviewBody);
    private const string ViewReviewBodiesView = nameof(ViewReviewBodies);
    private const string ConfirmChangesView = nameof(ConfirmChanges);
    private const string SuccessMessagesView = nameof(SuccessMessage);
    private const string ConfirmStatusView = nameof(ReviewBodyStatusChanges);
    private const string AuditTrailView = nameof(AuditTrail);
    private const string ConfirmAddRemoveUser = nameof(ConfirmAddRemoveUser);
    private const string SuccessAddRemoveUserMessageView = nameof(SuccessAddRemoveUserMessageView);

    private const string UpdateMode = "update";
    private const string CreateMode = "create";
    private const string DisableMode = "disable";
    private const string EnableMode = "enable";

    /// <summary>
    ///     Displays a list of review bodies
    /// </summary>
    [HttpGet]
    [HttpPost]
    [Route("/reviewbody/view", Name = "rbc:viewreviewbodies")]
    public async Task<IActionResult> ViewReviewBodies(
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ReviewBodyDto.RegulatoryBodyName),
        string? sortDirection = SortDirections.Ascending,
        [FromForm] ReviewBodySearchViewModel? model = null,
        [FromQuery] string? complexSearchQuery = null,
        [FromQuery] bool fromPagination = false)
    {
        if (fromPagination && !string.IsNullOrWhiteSpace(complexSearchQuery))
        {
            model ??= new ReviewBodySearchViewModel();
            model.Search = JsonSerializer.Deserialize<ReviewBodySearchModel>(complexSearchQuery);
        }
        else
        {
            // RESET ON SEARCH AND REMOVE FILTERS
            pageNumber = 1;
            pageSize = 20;
        }

        // Ensure model and model.Search are not null
        model ??= new ReviewBodySearchViewModel();
        model.Search ??= new ReviewBodySearchModel();

        var request = new ReviewBodySearchRequest
        {
            SearchQuery = model.Search.SearchQuery,
            Country = model.Search.Country,
            Status = model.Search.Status
        };

        var response = await reviewBodyService.GetAllReviewBodies(request, pageNumber, pageSize, sortField, sortDirection);

        var paginationModel = new PaginationViewModel(pageNumber, pageSize, response.Content?.TotalCount ?? 0)
        {
            RouteName = "rbc:viewreviewbodies",
            ComplexSearchQuery = model.Search,
            SortField = sortField,
            SortDirection = sortDirection
        };

        var reviewBodySearchViewModel = new ReviewBodySearchViewModel
        {
            Pagination = paginationModel,
            ReviewBodies = response.Content?.ReviewBodies,
            Search = model.Search
        };


        return View(reviewBodySearchViewModel);
    }


    /// <summary>
    ///     Displays a single review body
    /// </summary>
    [Route("/reviewbody/view/{id}", Name = "rbc:viewreviewbody")]
    public async Task<IActionResult> ViewReviewBody(Guid id)
    {
        var reviewBody = await reviewBodyService.GetReviewBodyById(id);

        var model = reviewBody.Content.Adapt<AddUpdateReviewBodyModel>();

        return View(model);
    }

    /// <summary>
    ///     Displays the empty review body to create
    /// </summary>
    [HttpGet]
    [Route("/reviewbody/create", Name = "rbc:createreviewbody")]
    public IActionResult CreateReviewBody()
    {
        ViewBag.Mode = CreateMode;
        var model = new AddUpdateReviewBodyModel();
        return View(CreateUpdateReviewBodyView, model);
    }

    /// <summary>
    ///     Displays the create / edit review body with data
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/create", Name = "rbc:createreviewbody")]
    public IActionResult CreateReviewBody(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.Id == Guid.Empty ? CreateMode : UpdateMode;
        return View(CreateUpdateReviewBodyView, model);
    }

    /// <summary>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/confirm-changes", Name = "rbc:confirmchanges")]
    public async Task<IActionResult> ConfirmChanges(AddUpdateReviewBodyModel model)
    {
        var context = new ValidationContext<AddUpdateReviewBodyModel>(model);
        var validationResult = await validator.ValidateAsync(context);

        if (validationResult.IsValid)
        {
            return View(ConfirmChangesView, model);
        }

        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return View(CreateUpdateReviewBodyView, model);
    }

    /// <summary>
    ///     Displays the edit CreateUpdateReviewBodyView when creating a review body
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/edit", Name = "rbc:editnewreviewbody")]
    public IActionResult EditNewReviewBody(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = CreateMode;
        return View(CreateUpdateReviewBodyView, model);
    }

    /// <summary>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/submit", Name = "rbc:submitreviewbody")]
    public async Task<IActionResult> SubmitReviewBody(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.Id == Guid.Empty ? CreateMode : UpdateMode;

        var context = new ValidationContext<AddUpdateReviewBodyModel>(model);
        var validationResult = await validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(CreateUpdateReviewBodyView, model);
        }

        var reviewBody = model.Adapt<ReviewBodyDto>();

        if (ViewBag.Mode == CreateMode)
        {
            reviewBody.CreatedBy = User?.Identity?.Name!;
            reviewBody.IsActive = true;
        }

        reviewBody.UpdatedBy = User?.Identity?.Name;

        var response = ViewBag.Mode == CreateMode
            ? await reviewBodyService.CreateReviewBody(reviewBody)
            : await reviewBodyService.UpdateReviewBody(reviewBody);

        if (response.IsSuccessStatusCode)
        {
            return ViewBag.Mode == CreateMode
                ? View(SuccessMessagesView, model)
                : RedirectToAction(ViewReviewBodyView, model);
        }

        // return error page as api wasn't successful
        return this.ServiceError(response);
    }

    [HttpGet]
    [Route("/reviewbody/success", Name = "rbc:sucessmessage")]
    public IActionResult SuccessMessage(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.Id == Guid.Empty ? CreateMode : UpdateMode;
        return View(SuccessMessagesView, model);
    }

    /// <summary>
    ///     Displays the update review body
    /// </summary>
    [Route("/reviewbody/update", Name = "rbc:updatereviewbody")]
    public async Task<IActionResult> UpdateReviewBody(Guid id)
    {
        var reviewBodyDto = await reviewBodyService.GetReviewBodyById(id);

        ViewBag.Mode = UpdateMode;
        var model = reviewBodyDto.Content;

        var addUpdateReviewBodyModel = model.Adapt<AddUpdateReviewBodyModel>();

        return View(CreateUpdateReviewBodyView, addUpdateReviewBodyModel);
    }

    /// <summary>
    ///     Displays the update review body
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/disable", Name = "rbc:disablereviewbody")]
    public async Task<IActionResult> DisableReviewBody(Guid id)
    {
        var reviewBodyDto = await reviewBodyService.GetReviewBodyById(id);

        ViewBag.Mode = DisableMode;
        var model = reviewBodyDto.Content;

        if (model == null)
        {
            return RedirectToAction(ViewReviewBodiesView);
        }

        model.IsActive = false;

        var addUpdateReviewBodyModel = model.Adapt<AddUpdateReviewBodyModel>();
        return View(ConfirmStatusView, addUpdateReviewBodyModel);
    }

    /// <summary>
    ///     Displays the update review body
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/enable", Name = "rbc:enablereviewbody")]
    public async Task<IActionResult> EnableReviewBody(Guid id)
    {
        var reviewBodyDto = await reviewBodyService.GetReviewBodyById(id);

        ViewBag.Mode = EnableMode;
        var model = reviewBodyDto.Content;

        if (model == null)
        {
            return RedirectToAction(ViewReviewBodiesView);
        }

        model.IsActive = true;
        var addUpdateReviewBodyModel = model.Adapt<AddUpdateReviewBodyModel>();

        return View(ConfirmStatusView, addUpdateReviewBodyModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/status", Name = "rbc:reviewbodystatuschanges")]
    public IActionResult ReviewBodyStatusChanges(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.IsActive ? EnableMode : DisableMode;
        return View(ConfirmStatusView, model);
    }

    /// <summary>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/reviewbody/confirm-status", Name = "rbc:confirmstatusupdate")]
    public async Task<IActionResult> ConfirmStatusUpdate(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.IsActive ? EnableMode : DisableMode;

        if (ViewBag.Mode == EnableMode)
        {
            await reviewBodyService.EnableReviewBody(model.Id);
            ViewBag.Mode = EnableMode;
        }
        else
        {
            await reviewBodyService.DisableReviewBody(model.Id);
            ViewBag.Mode = DisableMode;
        }

        return View(SuccessMessagesView, model);
    }

    [HttpGet]
    [Route("/reviewbody/audit-trail", Name = "rbc:audittrail")]
    public async Task<IActionResult> AuditTrail(Guid reviewBodyId, int pageNumber = 1, int pageSize = 20)
    {
        var skip = (pageNumber - 1) * pageSize;
        var take = pageNumber * pageSize;
        var response = await reviewBodyService.ReviewBodyAuditTrail(reviewBodyId, skip, take);

        var auditTrailResponse = response?.Content;
        var items = auditTrailResponse?.Items;

        var paginationModel = new PaginationViewModel(pageNumber, pageSize,
            auditTrailResponse != null ? auditTrailResponse.TotalCount : -1)
        {
            RouteName = "rbc:audittrail",
            AdditionalParameters =
            {
                { "reviewBodyId", reviewBodyId.ToString() }
            }
        };

        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);
        var reviewBodyName = reviewBody?.Content?.RegulatoryBodyName;

        var resultModel = new ReviewBodyAuditTrailViewModel
        {
            BodyName = reviewBodyName,
            Pagination = paginationModel,
            Items = items!
        };

        return View(AuditTrailView, resultModel);
    }

    /// <summary>
    ///     Displays users for a review body
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ViewReviewBodyUsers(Guid reviewBodyId, string? searchQuery = null,
        int pageNumber = 1, int pageSize = 20)
    {
        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);
        var reviewBodyModel = reviewBody.Content?.Adapt<AddUpdateReviewBodyModel>();

        var totalUserCount = 0;
        var model = new ReviewBodyListUsersModel
        {
            ReviewBody = reviewBodyModel!
        };

        if (reviewBody?.Content?.Users != null)
        {
            var userIds = reviewBody.Content?.Users?.Select(x => x.UserId.ToString());
            if (userIds != null && userIds.Any())
            {
                var users = await userService.GetUsersByIds(userIds,
                    searchQuery,
                    pageNumber,
                    pageSize);

                model.Users = users.Content?.Users.Select(user => new UserViewModel(user)) ?? [];

                totalUserCount = users.Content?.TotalCount ?? 0;
            }
        }

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, totalUserCount)
        {
            SearchQuery = searchQuery,
            RouteName = "rbc:viewreviewbodyusers",
            AdditionalParameters =
            {
                { "reviewBodyId", reviewBodyId.ToString() }
            }
        };

        return View(model);
    }

    /// <summary>
    ///     Displays page for adding a user to review body
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ViewAddUser(Guid reviewBodyId, string? searchQuery = null, int pageNumber = 1,
        int pageSize = 20)
    {
        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);

        var reviewBodyModel = reviewBody.Content?.Adapt<AddUpdateReviewBodyModel>();

        var model = new ReviewBodyListUsersModel
        {
            ReviewBody = reviewBodyModel!
        };
        var existingUserIds = reviewBody.Content?.Users?.Select(x => x.UserId.ToString()) ?? [];

        if (!string.IsNullOrEmpty(searchQuery))
        {
            // search all users
            var users = await userService.SearchUsers(searchQuery, existingUserIds, pageNumber, pageSize);

            model.Users = users.Content?.Users.Select(user => new UserViewModel(user)) ?? [];

            model.Pagination = new PaginationViewModel(pageNumber, pageSize, users.Content?.TotalCount ?? 0)
            {
                RouteName = "rbc:viewadduser",
                SearchQuery = searchQuery,
                AdditionalParameters =
                {
                    { "reviewBodyId", reviewBodyId.ToString() }
                }
            };
        }

        return View(model);
    }

    /// <summary>
    ///     Displays confirmation page before adding a user to a review body
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmAddUser(Guid reviewBodyId, Guid userId)
    {
        var model = new ConfirmAddRemoveReviewBodyUserModel();

        // get review body
        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);
        var reviewBodyModel = reviewBody.Content?.Adapt<AddUpdateReviewBodyModel>();

        // get selected user
        var user = await userService.GetUser(userId.ToString(), null);

        model.ReviewBody = reviewBodyModel ?? new AddUpdateReviewBodyModel();
        model.User = user.Content != null ? new UserViewModel(user.Content) : new UserViewModel();

        return View(ConfirmAddRemoveUser, model);
    }

    /// <summary>
    ///     Adds a user to a review body
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitAddUser(Guid reviewBodyId, Guid userId)
    {
        var reviewBodyUserDto = new ReviewBodyUserDto
        {
            Id = reviewBodyId,
            UserId = userId,
            DateAdded = DateTime.UtcNow
        };

        await reviewBodyService.AddUserToReviewBody(reviewBodyUserDto);

        // get review body
        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);
        var reviewBodyModel = reviewBody.Content?.Adapt<AddUpdateReviewBodyModel>();

        // get selected user
        var user = await userService.GetUser(userId.ToString(), null);

        var model = new ConfirmAddRemoveReviewBodyUserModel
        {
            User = user.Content != null ? new UserViewModel(user.Content) : new UserViewModel(),
            ReviewBody = reviewBodyModel ?? new AddUpdateReviewBodyModel()
        };

        return View(SuccessAddRemoveUserMessageView, model);
    }

    /// <summary>
    ///     Displays confirmation page before removing a user from a review body
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmRemoveUser(Guid reviewBodyId, Guid userId)
    {
        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);
        var reviewBodyModel = reviewBody.Content?.Adapt<AddUpdateReviewBodyModel>();
        var user = await userService.GetUser(userId.ToString(), null);

        var model = new ConfirmAddRemoveReviewBodyUserModel
        {
            ReviewBody = reviewBodyModel ?? new AddUpdateReviewBodyModel(),
            User = user.Content != null ? new UserViewModel(user.Content) : new UserViewModel(),
            IsRemove = true
        };

        ViewBag.Style = "govuk-button govuk-button--warning";

        return View(ConfirmAddRemoveUser, model);
    }

    /// <summary>
    ///     Removes a user from a review body
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitRemoveUser(Guid reviewBodyId, Guid userId)
    {
        await reviewBodyService.RemoveUserFromReviewBody(reviewBodyId, userId);

        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);
        var reviewBodyModel = reviewBody.Content?.Adapt<AddUpdateReviewBodyModel>();
        var user = await userService.GetUser(userId.ToString(), null);

        var model = new ConfirmAddRemoveReviewBodyUserModel
        {
            User = user.Content != null ? new UserViewModel(user.Content) : new UserViewModel(),
            ReviewBody = reviewBodyModel ?? new AddUpdateReviewBodyModel(),
            IsRemove = true
        };

        return View(SuccessAddRemoveUserMessageView, model);
    }

    [HttpGet]
    [Route("/reviewbody/clearfilters", Name = "rbc:clearfilters")]
    public IActionResult ClearFilters([FromQuery] string? searchQuery = null)
    {
        var cleanedSearch = new ReviewBodySearchModel
        {
            SearchQuery = searchQuery
        };

        var searchJson = JsonSerializer.Serialize(cleanedSearch);

        return RedirectToRoute("rbc:viewreviewbodies", new
        {
            pageNumber = 1,
            pageSize = 20,
            complexSearchQuery = searchJson,
            fromPagination = true
        });
    }

    [HttpGet]
    [Route("/reviewbody/removefilter", Name = "rbc:removefilter")]
    public IActionResult RemoveFilter(string key, string? value, [FromQuery] string? model = null)
    {
        var viewModel = new ReviewBodySearchViewModel();

        if (!string.IsNullOrWhiteSpace(model))
        {
            viewModel.Search = JsonSerializer.Deserialize<ReviewBodySearchModel>(model);
        }
        else
        {
            viewModel.Search = new ReviewBodySearchModel();
        }

        switch (key.ToLowerInvariant().Replace(" ", ""))
        {
            case "country":
                if (!string.IsNullOrEmpty(value) && viewModel.Search.Country != null)
                {
                    viewModel.Search.Country = viewModel.Search.Country
                        .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                break;

            case "status":
                viewModel.Search.Status = null;
                break;
        }

        // Serialize modified search model to JSON for complexSearchQuery parameter
        var searchJson = JsonSerializer.Serialize(viewModel.Search);

        // Redirect to ViewReviewBodies with query parameters
        return RedirectToRoute("rbc:viewreviewbodies", new
        {
            pageNumber = 1,
            pageSize = 20,
            complexSearchQuery = searchJson,
            fromPagination = true
        });
    }
}