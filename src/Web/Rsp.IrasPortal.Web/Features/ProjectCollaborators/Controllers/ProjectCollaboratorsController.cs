using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.IrasPortal.Web.Features.ProjectCollaborators.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Features.ProjectCollaborators.Controllers;

/// <summary>
/// Manages project collaborators, including searching for users and adding them to projects.
/// </summary>
[Route("[controller]/[action]", Name = "col:[action]")]
[Authorize(Policy = Workspaces.MyResearch)]
public class ProjectCollaboratorsController
(
    IProjectCollaboratorService projectCollaboratorService
) : Controller
{
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    [RequireCollaboratorAccess("Edit")]
    [FeatureGate(FeatureFlags.TeamRoles)]
    [HttpPost]
    public async Task<IActionResult> SaveCollaborator(CollaboratorViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ProjectAccessLevel))
        {
            ModelState.AddModelError(nameof(model.ProjectAccessLevel), "Please select access level");

            return View("CollaboratorAccess", model);
        }

        var response = await projectCollaboratorService.SaveProjectCollaborator(new ProjectCollaboratorRequest
        {
            ProjectRecordId = model.ProjectRecordId!,
            UserId = model.UserId!,
            ProjectAccessLevel = model.ProjectAccessLevel!
        });

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        TempData[TempDataKeys.ProjectCollaborators.OperationMessage] = "Collaborator added";

        return RedirectToRoute("pov:projectteam", new
        {
            model.ProjectRecordId
        });
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    [RequireCollaboratorAccess("Edit")]
    [FeatureGate(FeatureFlags.TeamRoles)]
    public async Task<IActionResult> SelectCollaboratorAccess(CollaboratorViewModel model)
    {
        return View("CollaboratorAccess", model);
    }

    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    [RequireCollaboratorAccess("Edit")]
    [FeatureGate(FeatureFlags.TeamRoles)]
    [HttpPost]
    public async Task<IActionResult> UpdateCollaboratorAccess(CollaboratorViewModel model)
    {
        var response = await projectCollaboratorService.UpdateCollaboratorAccess(new UpdateCollaboratorAccessRequest
        {
            Id = model.Id!,
            ProjectAccessLevel = model.ProjectAccessLevel!
        });

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        TempData[TempDataKeys.ProjectCollaborators.OperationMessage] = "Collaborator access changed";

        return RedirectToRoute("pov:projectteam", new { model.ProjectRecordId });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    [FeatureGate(FeatureFlags.TeamRoles)]
    public async Task<IActionResult> RemoveCollaborator(CollaboratorViewModel model)
    {
        return View(model);
    }

    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    [FeatureGate(FeatureFlags.TeamRoles)]
    [HttpPost]
    public async Task<IActionResult> ConfirmRemoveCollaborator(CollaboratorViewModel model)
    {
        var response = await projectCollaboratorService.RemoveProjectCollaborator(model.Id!);

        if (!response.IsSuccessStatusCode)
        {
            return this.ServiceError(response);
        }

        if (model.Self)
        {
            var shortProjectTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? "";

            TempData[TempDataKeys.ProjectCollaborators.OperationMessage] = $"You removed yourself as a collaborator on {shortProjectTitle}";

            return RedirectToRoute("app:welcome");
        }

        TempData[TempDataKeys.ProjectCollaborators.OperationMessage] = "Collaborator removed";

        return RedirectToRoute("pov:projectteam", new { model.ProjectRecordId });
    }
}