using System.Net;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public partial class ProjectModificationController
{
    [HttpGet]
    public IActionResult ModificationDetailsPage()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsPageViewModel());

        viewModel.ModificationId = Guid.NewGuid();
        viewModel.Status = "Draft";
        viewModel.ModificationType = "Minor modification";
        viewModel.Category = "{A > B/C > B > C > New site > N/A}";
        viewModel.ReviewType = "No review required";
        viewModel.ModificationChanges = new List<ModificationChangeModel>
        {
            new ModificationChangeModel
            {
                ModificationChangeId = Guid.NewGuid(),
                ModificationType = "Minor Modification",
                Category = "A > B/C",
                ReviewType = "No review required",
                AreaOfChangeName = "Planned End Date",
                SpecificChangeName = "PlannedEndDateChanged",
                SpecificChangeAnswer = "The planned end date has been moved by 3 months.",
                ChangeStatus = "Change ready for submission"
            },
            new ModificationChangeModel
            {
                ModificationChangeId = Guid.NewGuid(),
                ModificationType = "Minor Modification",
                Category = "New site",
                ReviewType = "No review required",
                AreaOfChangeName = "Modification Document",
                SpecificChangeName = "Other Modification Change",
                SpecificChangeAnswer = "Additional documentation has been submitted for review.",
                ChangeStatus = "Change ready for submission"
            }
        };

        if (viewModel.ModificationChanges.All(c => c.ChangeStatus == "Change ready for submission"))
        {
            viewModel.ChangesReadyForSubmission = true;
        }

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult UnfinishedChanges()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsPageViewModel());
        return View("UnfinishedChanges", viewModel);
    }

    [HttpGet]
    public IActionResult ConfirmRemoveChange(string modificationChangeId)
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsPageViewModel());
        return View("ConfirmRemoveChange", (viewModel, modificationChangeId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveChange(string modificationChangeId)
    {
        var removeChangeResponse = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        if (!removeChangeResponse.IsSuccessStatusCode)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(ModificationDetailsPage));
    }
}