using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "rbc:[action]")]
[Authorize(Policy = "IsAdmin")]
public class ReviewBodyController(IReviewBodyService reviewBodyService, IValidator<AddUpdateReviewBodyModel> validator)
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

    private const string UpdateMode = "update";
    private const string CreateMode = "create";
    private const string DisableMode = "disable";
    private const string EnableMode = "enable";

    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    ///     Displays a list of review bodies
    /// </summary>
    public async Task<IActionResult> ViewReviewBodies()
    {
        var reviewBodies = await reviewBodyService.GetAllReviewBodies();

        return View(reviewBodies.Content?.OrderBy(rb => rb.OrganisationName));
    }

    /// <summary>
    ///     Displays a single review body
    /// </summary>
    public async Task<IActionResult> ViewReviewBody(Guid id)
    {
        var reviewBody = await reviewBodyService.GetReviewBodyById(id);

        var model = reviewBody.Content?.FirstOrDefault().Adapt<AddUpdateReviewBodyModel>();

        return View(model);
    }

    /// <summary>
    ///     Displays the empty review body to create
    /// </summary>
    [HttpGet]
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
    public async Task<IActionResult> EditNewReviewBody(AddUpdateReviewBodyModel model)
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

        //TODO: WE NEED TO HANDLE ERRORS HERE
        return View(SuccessMessagesView, model);
    }

    [HttpGet]
    public IActionResult SuccessMessage(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.Id == Guid.Empty ? CreateMode : UpdateMode;
        return View(SuccessMessagesView, model);
    }

    /// <summary>
    ///     Displays the update review body
    /// </summary>
    public async Task<IActionResult> UpdateReviewBody(Guid id)
    {
        var reviewBodyDto = await reviewBodyService.GetReviewBodyById(id);

        ViewBag.Mode = UpdateMode;
        var model = reviewBodyDto.Content?.FirstOrDefault();

        var addUpdateReviewBodyModel = model.Adapt<AddUpdateReviewBodyModel>();

        return View(CreateUpdateReviewBodyView, addUpdateReviewBodyModel);
    }

    /// <summary>
    ///     Displays the update review body
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableReviewBody(Guid id)
    {
        var reviewBodyDto = await reviewBodyService.GetReviewBodyById(id);

        ViewBag.Mode = DisableMode;
        var model = reviewBodyDto.Content?.FirstOrDefault();

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
    public async Task<IActionResult> EnableReviewBody(Guid id)
    {
        var reviewBodyDto = await reviewBodyService.GetReviewBodyById(id);

        ViewBag.Mode = EnableMode;
        var model = reviewBodyDto.Content?.FirstOrDefault();

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
    public async Task<IActionResult> AuditTrail(Guid reviewBodyId, int pageNumber = 1, int pageSize = 10)
    {
        var skip = (pageNumber - 1) * pageSize;
        var take = pageNumber * pageSize;
        var response = await reviewBodyService.ReviewBodyAuditTrail(reviewBodyId, skip, take);

        var auditTrailResponse = response?.Content;
        var items = auditTrailResponse?.Items;

        var paginationModel = new PaginationViewModel
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            RouteName = "rbc:audittrail",
            TotalCount = auditTrailResponse != null ? auditTrailResponse.TotalCount : -1,
            ReviewBodyId = reviewBodyId.ToString()
        };

        var reviewBody = await reviewBodyService.GetReviewBodyById(reviewBodyId);
        var reviewBodyName = reviewBody?.Content?.FirstOrDefault()?.OrganisationName;

        var resultModel = new ReviewBodyAuditTrailViewModel
        {
            BodyName = reviewBodyName,
            Pagination = paginationModel,
            Items = items!
        };

        return View(AuditTrailView, resultModel);
    }
}