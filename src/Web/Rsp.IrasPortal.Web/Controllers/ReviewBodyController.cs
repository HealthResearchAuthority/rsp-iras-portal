using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "rbc:[action]")]
[Authorize(Policy = "IsUser")]
public class ReviewBodyController(IReviewBodyService reviewBodyService) : Controller
{
    private const string Error = nameof(Error);
    private const string CreateEditReviewBodyView = nameof(CreateReviewBody);
    private const string ConfirmChangesView = nameof(ConfirmChanges);
    private const string SuccessMessagesView = nameof(SuccessMessage);
    private const string DisableMessagesView = nameof(SuccessMessage);

    private const string EditMode = "edit";
    private const string CreateMode = "create";
    private const string DisableMode = "disable";

    /// <summary>
    /// Displays a list of review bodies
    /// </summary>
    public async Task<IActionResult> ViewReviewBodies()
    {
        var reviewBodies = await reviewBodyService.GetAllReviewBodies();

        return View(reviewBodies.Content?.OrderBy(rb => rb.OrganisationName));
    }

    /// <summary>
    /// Displays a single review body
    /// </summary>
    public async Task<IActionResult> ViewReviewBody(Guid id)
    {
        var reviewBody = await reviewBodyService.GetReviewBodyById(id);

        return View(reviewBody.Content?.FirstOrDefault());
    }

    /// <summary>
    ///     Displays the empty review body to create
    /// </summary>
    [HttpGet]
    public IActionResult CreateReviewBody()
    {
        ViewBag.Mode = CreateMode;
        var model = new AddUpdateReviewBodyModel();
        return View(CreateEditReviewBodyView, model);
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
        ViewBag.Mode = CreateMode;
        return View(CreateEditReviewBodyView, model);
    }

    /// <summary>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmChanges(AddUpdateReviewBodyModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(CreateEditReviewBodyView, model);
        }

        return View(ConfirmChangesView, model);
    }

    /// <summary>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReviewBody(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.Id == Guid.Empty ? CreateMode : EditMode;

        var response = await reviewBodyService.CreateReviewBody(new ReviewBodyDto
        {
            OrganisationName = model.OrganisationName,
            Countries = model.Countries,
            EmailAddress = model.EmailAddress,
            Description = model.Description!,
            CreatedBy = User?.Identity?.Name!,
            UpdatedBy = User?.Identity?.Name!,
            IsActive = true
        });


        if (response.IsSuccessStatusCode)
        {
            return View(SuccessMessagesView, model);
        }

        // WE NEED TO HANDLE ERRORS HERE
        return View(SuccessMessagesView, model);
    }

    [HttpGet]
    public IActionResult SuccessMessage(AddUpdateReviewBodyModel model)
    {
        ViewBag.Mode = model.Id == Guid.Empty ? CreateMode : EditMode;
        return View(SuccessMessagesView, model);
    }

    [HttpGet]
    public IActionResult DisableReviewBody(AddUpdateReviewBodyModel model)
    {
        // NEED TO IMPLEMENT PAGES
        ViewBag.Mode = DisableMode;
        return View(DisableMessagesView, model);
    }
}