using Rsp.Portal.Application.Constants;
using Rsp.Portal.Domain.AccessControl;

namespace Rsp.Portal.Application.AccessControl;

/// <summary>
/// Provides a mapping between workspace identifiers and the roles that are allowed to access each workspace.
/// </summary>
public static class WorkspaceRolesMatrix
{
    /// <summary>
    /// Dictionary mapping workspace keys (from <see cref="Workspaces"/>) to an array of role keys
    /// (from <see cref="Roles"/>) that are permitted in that workspace.
    ///
    /// Each entry:
    /// - Key: workspace identifier string (e.g. <see cref="Workspaces.MyResearch"/>)
    /// - Value: array of role identifier strings that are allowed to view/use the workspace
    /// </summary>
    private static readonly Dictionary<string, string[]> _workspaceRoles = new()
    {
        // All Roles allowed in the "Profile" workspace:
        [Workspaces.Profile] =
        [
            Roles.Applicant, Roles.Sponsor, Roles.WorkflowCoordinator, Roles.TeamManager, Roles.SystemAdministrator,Roles.StudyWideReviewer,Roles.OrganisationAdministrator
        ],

        // Roles allowed in the "My Research" workspace:
        // - Applicants (owners of applications)
        // - Sponsors (sponsor users)
        // - Workflow coordinators (manage progress)
        // - Team managers (team-level reviewers)
        // - System administrators (full access)
        [Workspaces.MyResearch] =
        [
            Roles.Applicant, Roles.Sponsor, Roles.WorkflowCoordinator, Roles.TeamManager, Roles.SystemAdministrator,Roles.StudyWideReviewer,Roles.OrganisationAdministrator
        ],

        // Roles allowed in the "Sponsor" workspace:
        // - Sponsor users and system administrators only
        [Workspaces.Sponsor] =
        [
            Roles.Sponsor, Roles.SystemAdministrator, Roles.OrganisationAdministrator
        ],

        // Roles allowed in the "System Administration" workspace:
        // - System administrators only
        [Workspaces.SystemAdministration] =
        [
            Roles.SystemAdministrator
        ],

        // Roles allowed in the "Approvals" workspace:
        // - Team managers, study-wide reviewers, workflow coordinators and admins
        [Workspaces.Approvals] =
        [
            Roles.TeamManager, Roles.StudyWideReviewer, Roles.WorkflowCoordinator, Roles.SystemAdministrator
        ]
    };

    public static Dictionary<string, string[]> WorkspaceRoles { get; } = _workspaceRoles;
}