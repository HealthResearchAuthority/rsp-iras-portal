using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
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
[Route("projectcollaborators/[action]", Name = "col:[action]")]
[Authorize(Policy = Workspaces.MyResearch)]
public class ProjectCollaboratorsSearchController
(
    IUserManagementService userManagementService,
    IProjectCollaboratorService projectCollaboratorService,
    ISponsorOrganisationService sponsorOrganisationService,
    IRespondentService respondentService
) : Controller
{
    /// <summary>
    /// Displays the form to search for a potential collaborator by email.
    /// Requires project update permission and the TeamRoles feature flag to be enabled.
    /// </summary>
    /// <param name="projectRecordId">The ID of the project to add a collaborator to</param>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    [RequireCollaboratorAccess("Edit")]
    [FeatureGate(FeatureFlags.TeamRoles)]
    public async Task<IActionResult> AddCollaborator(string projectRecordId)
    {
        return View(new SearchCollaboratorViewModel { ProjectRecordId = projectRecordId });
    }

    /// <summary>
    /// Searches for a user by email and validates whether they can be added as a project collaborator.
    /// Performs multiple validation checks to ensure the user meets all requirements.
    /// </summary>
    /// <param name="model">Contains the project ID and email to search for</param>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Search)]
    [RequireCollaboratorAccess("Edit")]
    [FeatureGate(FeatureFlags.TeamRoles)]
    public async Task<IActionResult> SearchCollaborator(SearchCollaboratorViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Enter an email address");

            return View(nameof(AddCollaborator), model);
        }

        // Fetch existing collaborators and user details in parallel for performance
        var getCollaboratorsTask = projectCollaboratorService.GetProjectCollaborators(model.ProjectRecordId);
        var getUserTask = userManagementService.GetUser(null, model.Email);

        await Task.WhenAll(getCollaboratorsTask, getUserTask);

        var collaboratorsResponse = await getCollaboratorsTask;
        var getUserResponse = await getUserTask;

        // Handle service errors for collaborators request
        if (!collaboratorsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(collaboratorsResponse);
        }

        // Handle service errors for user request (except NotFound which is expected when user doesn't exist)
        if (!getUserResponse.IsSuccessStatusCode && getUserResponse.StatusCode != HttpStatusCode.NotFound)
        {
            return this.ServiceError(getUserResponse);
        }

        // If user doesn't exist in the system, mark as not found
        if (getUserResponse.Content?.User == null)
        {
            model.CollaboratorFound = false;
            return View(nameof(AddCollaborator), model);
        }

        var collaborators = collaboratorsResponse.Content ?? [];
        var user = getUserResponse.Content!.User;
        var roles = getUserResponse.Content.Roles;

        // Check if user is already a collaborator on this project
        model.IsExistingCollaborator =
            collaborators.Any(c => c.UserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase));

        // Default to invalid until all validation checks pass - simplifies view logic
        model.InvalidUser = true;

        // Validation #1: User cannot be added if already a collaborator
        if (model.IsExistingCollaborator == true)
        {
            model.InvalidUserMessage = "This user is already a collaborator on the project.";
            return View(nameof(AddCollaborator), model);
        }

        // Fetch project respondent answers to check Chief Investigator and Sponsor details
        var respondentServiceResponse = await respondentService.GetRespondentAnswers(model.ProjectRecordId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(respondentServiceResponse);
        }

        var respondentAnswers = respondentServiceResponse.Content ?? [];

        // Extract sponsor organisation ID from project answers
        var sponsorOrganisationId = respondentAnswers
            .FirstOrDefault(answer => answer.QuestionId == QuestionIds.PrimarySponsorOrganisation)?
            .AnswerText;

        // Validation #2: Check if user is a sponsor on the project
        // Sponsors cannot be collaborators to avoid role conflicts
        var sponsorOrganisationServiceResponse = await sponsorOrganisationService.GetUserInSponsorOrganisation(sponsorOrganisationId!, Guid.Parse(user.Id!));

        if (!sponsorOrganisationServiceResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(sponsorOrganisationServiceResponse);
        }

        var sponsorOrganisationUser = sponsorOrganisationServiceResponse.Content;

        if (sponsorOrganisationUser != null)
        {
            model.InvalidUserMessage = "This user is a Sponsor on the project and cannot be added as a collaborator.";
            return View(nameof(AddCollaborator), model);
        }

        // Validation #3: Check if user is the Chief Investigator on the project
        // Chief Investigators cannot be collaborators to maintain clear project leadership
        var chiefInvestigatorEmail = respondentAnswers
            .FirstOrDefault(answer => answer.QuestionId == QuestionIds.ChiefInvestigatorEmail)?
            .AnswerText;

        if (chiefInvestigatorEmail?.Equals(model.Email, StringComparison.OrdinalIgnoreCase) == true)
        {
            model.InvalidUserMessage = "This user is a Chief Investigator on the project and cannot be added as a collaborator.";
            return View(nameof(AddCollaborator), model);
        }

        // Validation #4: User must have exactly one role and it must be Applicant
        // This ensures only appropriate users can be collaborators
        if (roles.Count(role => role == Roles.Applicant) != 1)
        {
            model.InvalidUserMessage = "Only users with the Applicant role can be added as a collaborator.";
            return View(nameof(AddCollaborator), model);
        }

        // All validation checks passed - user can be added as a collaborator
        model.CollaboratorFound = true;
        model.InvalidUser = false;
        model.UserId = user.Id;

        return View(nameof(AddCollaborator), model);
    }
}