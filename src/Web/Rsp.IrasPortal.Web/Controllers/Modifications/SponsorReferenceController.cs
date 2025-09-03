using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public partial class ProjectModificationController
{
    [HttpGet]
    public IActionResult SponsorReference()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new SponsorReferenceViewModel());
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSponsorReference(SponsorReferenceViewModel model, bool saveForLater = false)
    {
        var validationResult = await sponsorReferenceViewModelValidator.ValidateAsync(new ValidationContext<SponsorReferenceViewModel>(model));

        if (!validationResult.IsValid)
        {
            // Add validation errors to the ModelState
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            // Return the view with validation errors
            return View(nameof(SponsorReference), model);
        }

        if (saveForLater)
        {
            return RedirectToRoute("pov:postapproval", new { model.ProjectRecordId });
        }

        return RedirectToAction(nameof(ReviewAllChanges));
    }
}