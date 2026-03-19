using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Controllers;

[FeatureGate(FeatureFlags.RequestForInformation)]
[Authorize(Policy = Workspaces.MyResearch)]
[Route("/modifications/rfi/[action]", Name = "rfi:[action]")]
public class RfiResponseController(
    IProjectModificationsService projectModificationsService,
    IApplicationsService projectRecordService
    ) : Controller
{
    [HttpGet]
    public async Task<IActionResult> RfiDetails(string projectId, Guid modificationId)
    {
        var model = new RfiDetailsViewModel();

        // get modification details
        var modification = await projectModificationsService.GetModification(projectId, modificationId);
        var projectRecord = await projectRecordService.GetProjectRecord(projectId);

        var rfiResons = await projectModificationsService.GetModificationReviewResponses(projectId, modificationId);

        if (!modification.IsSuccessStatusCode ||
            modification.Content == null ||
            !projectRecord.IsSuccessStatusCode ||
            projectRecord.Content == null ||
            !rfiResons.IsSuccessStatusCode)
        {
            this.ServiceError(modification);
        }

        // only allow access to RFI details page when the modification is in RFI status,
        // otherwise return 403 forbidden
        if (modification.Content!.Status != ModificationStatus.RequestForInformation)
        {
            return Forbid();
        }

        model.IrasId = projectRecord.Content!.IrasId.ToString();
        model.ModificationId = modification.Content.ModificationIdentifier;
        model.ShortProjectTitle = projectRecord.Content.ShortProjectTitle;
        model.DateSubmitted = modification.Content.SentToRegulatorDate?.ToString("dd MMMM yyyy");
        model.RfiReasons = rfiResons.Content == null ? [] : rfiResons.Content.RequestForInformationReasons;
        model.ProjectId = projectId;
        model.ModificationGuid = modificationId.ToString();

        return View(model);
    }
}