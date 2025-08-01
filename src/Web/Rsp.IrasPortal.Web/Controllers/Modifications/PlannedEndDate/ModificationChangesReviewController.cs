using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
public partial class ProjectModificationController
{
    /// <summary>
    /// Displays the review page for planned end date modification changes.
    /// Populates the view model with project and modification metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult ModificationChangesReview()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new AffectingOrganisationsViewModel());

        return View(viewModel);
    }

    /// <summary>
    /// Handles the back navigation from the review screen.
    /// Removes the review changes TempData entry to allow proper navigation flow.
    /// Redirects to the affecting organisations page.
    /// </summary>
    [HttpGet]
    public IActionResult Back()
    {
        // Remove the review changes flag so the user can continue navigating back
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges);

        return RedirectToRoute("pmc:affectingorganisations");
    }
}