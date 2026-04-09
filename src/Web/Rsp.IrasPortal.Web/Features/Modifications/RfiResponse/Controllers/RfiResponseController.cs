using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
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

        var rfiReasons = await projectModificationsService.GetModificationReviewResponses(projectId, modificationId);
        var rfiResponses = await projectModificationsService.GetModificationRfiResponses(projectId, modificationId);

        if (!modification.IsSuccessStatusCode ||
            modification.Content == null ||
            !projectRecord.IsSuccessStatusCode ||
            projectRecord.Content == null ||
            !rfiReasons.IsSuccessStatusCode ||
            !rfiResponses.IsSuccessStatusCode)
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
        model.RfiReasons = rfiReasons.Content == null ? [] : rfiReasons.Content.RequestForInformationReasons;
        model.RfiResponses = rfiResponses.Content == null ? [] : rfiResponses.Content.RfiResponses;
        model.ProjectId = projectId;
        model.ModificationGuid = modificationId.ToString();

        if (model.RfiResponses.Count < model.RfiReasons.Count)
        {
            // pre-populate the RFI responses list with empty strings to match the count of RFI reasons
            model.RfiResponses.AddRange(Enumerable.Repeat(string.Empty, model.RfiReasons.Count - model.RfiResponses.Count));
        }

        TempData[TempDataKeys.RfiDetails] = JsonSerializer.Serialize(model);

        return View(model);
    }

    [HttpGet]
    public IActionResult RfiResponses()
    {
        var jsonModel = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;

        var model = JsonSerializer.Deserialize<RfiDetailsViewModel>(jsonModel)!;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RfiResponses(RfiDetailsViewModel model, bool saveForLater = false)
    {
        var storedModelJson = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;
        var storedModel = JsonSerializer.Deserialize<RfiDetailsViewModel>(storedModelJson)!;

        storedModel.RfiResponses = model.RfiResponses.ConvertAll(r => r ?? string.Empty);

        if (!saveForLater && storedModel.RfiResponses.Any(string.IsNullOrEmpty))
        {
            ModelState.AddModelError(string.Empty, "You have not provided a reason. Enter the reason for requesting further information from the applicant before you continue.");
            return View(storedModel);
        }

        var saveResponsesResponse = await projectModificationsService.SaveModificationRfiResponses(
            new ModificationRfiResponseRequest()
            {
                ProjectModificationId = Guid.Parse(storedModel.ModificationGuid!),
                Responses = [.. storedModel.RfiResponses],
            }
        );

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        if (saveForLater)
        {
            TempData.Remove(TempDataKeys.RfiDetails);
            return RedirectToAction(
                "ReviewAllChanges",
                "ReviewAllChanges",
                new
                {
                    projectRecordId = storedModel.ProjectId,
                    irasId = storedModel.IrasId,
                    shortTitle = storedModel.ShortProjectTitle,
                    projectModificationId = Guid.Parse(storedModel.ModificationGuid!),
                }
            );
        }

        TempData[TempDataKeys.RfiDetails] = JsonSerializer.Serialize(storedModel);

        return RedirectToAction(nameof(RfiCheckAndSubmitResponses));
    }

    [HttpGet]
    public IActionResult RfiCheckAndSubmitResponses()
    {
        var jsonModel = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;

        var model = JsonSerializer.Deserialize<RfiDetailsViewModel>(jsonModel)!;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RfiSubmitResponses()
    {
        var storedModelJson = TempData.Peek(TempDataKeys.RfiDetails)!.ToString()!;
        var storedModel = JsonSerializer.Deserialize<RfiDetailsViewModel>(storedModelJson)!;

        var updateStatusResponse = await projectModificationsService.UpdateModificationStatus
        (
            storedModel.ProjectId!,
            Guid.Parse(storedModel.ModificationGuid!),
            ModificationStatus.ResponseWithSponsor
        );

        if (!updateStatusResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateStatusResponse);
        }

        return RedirectToAction(nameof(RfiResponsesConfirmation));
    }

    [HttpGet]
    public IActionResult RfiResponsesConfirmation()
    {
        return View();
    }
}