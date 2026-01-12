using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Features.Approvals.RecordSearch.Models;

namespace Rsp.Portal.Web.Features.Approvals.RecordSearch.Controllers;

[Authorize(Policy = Workspaces.Approvals)]
[Route("approvals/[controller]/[action]", Name = "recordsearch:[action]")]
public class RecordSearchController(IValidator<RecordSearchNavigationModel> validator) : Controller
{
    [HttpGet("/approvals/[controller]", Name = "recordsearch")]
    public IActionResult Index()
    {
        return View(new RecordSearchNavigationModel());
    }

    [HttpPost]
    [CmsContentAction(nameof(Index))]
    public async Task<IActionResult> Navigate(RecordSearchNavigationModel model)
    {
        var context = new ValidationContext<RecordSearchNavigationModel>(model);
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

            return View(nameof(Index), model);
        }

        // redirect user based on their selected record type
        return model.RecordType switch
        {
            SearchRecordTypes.ProjectRecord => RedirectToRoute("projectrecordsearch"),
            SearchRecordTypes.ModificationRecord => RedirectToRoute("approvals:index"),
            _ => RedirectToAction(nameof(Index)),
        };
    }
}