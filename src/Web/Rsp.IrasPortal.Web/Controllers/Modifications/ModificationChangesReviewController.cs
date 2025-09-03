using System.Net;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
public partial class ProjectModificationController
{
    /// <summary>
    /// Retrieves the journey type from TempData and displays the review page.
    /// Populates the view model with project and modification metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult ModificationChangesReview()
    {
        var journeyType = TempData.Peek(TempDataKeys.ProjectModification.JourneyType) as string;

        return journeyType switch
        {
            ModificationJourneyTypes.PlannedEndDate => View("PlannedEndDateReview",
                TempData.PopulateBaseProjectModificationProperties(new AffectingOrganisationsViewModel())),

            _ => this.ServiceError(new ServiceResponse()
                .WithError($"Missing or invalid journey type: {journeyType}", "Bad Request", HttpStatusCode.BadRequest))
        };
    }

    /// <summary>
    /// Handles the back navigation from the review screen.
    /// Retrieves the journey type from TempData and redirects to the appropriate step in the modification flow.
    /// If the journey type is missing or invalid, returns a Bad Request error view.
    /// </summary>
    [HttpGet]
    public IActionResult Back()
    {
        var journeyType = TempData.Peek(TempDataKeys.ProjectModification.JourneyType) as string;

        // Remove journey specific flags
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges);

        return journeyType switch
        {
            ModificationJourneyTypes.PlannedEndDate => RedirectToAction(nameof(AffectingOrganisations)),
            _ => this.ServiceError(new ServiceResponse()
                .WithError($"Missing or invalid journey type: {journeyType}", "Bad Request", HttpStatusCode.BadRequest))
        };
    }
}