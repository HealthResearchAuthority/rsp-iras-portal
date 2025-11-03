using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Features.Approvals.RecordSearch.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Approvals.ProjectRecordSearch.Controllers;

[Route("[controller]/[action]", Name = "recordsearch:[action]")]
[Authorize]
public class RecordSearchController(IValidator<RecordSearchNavigationModel> validator) : Controller
{
    [HttpGet("~/[controller]", Name = "recordsearch")]
    public IActionResult Index()
    {
        return View("~/Features/Approvals/RecordSearch/Views/Index.cshtml", new RecordSearchNavigationModel());
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

            return View("~/Features/Approvals/RecordSearch/Views/Index.cshtml", model);
        }

        // redirect user based on their selected record type
        switch (model.RecordType)
        {
            case SearchRecordTypes.ProjectRecord:
                return RedirectToAction(nameof(Index));

            case SearchRecordTypes.ModificationRecord:
                return RedirectToAction(nameof(ApprovalsController.Index), "Approvals");
        }

        return RedirectToAction(nameof(Index));
    }
}