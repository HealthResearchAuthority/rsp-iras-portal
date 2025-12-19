using Rsp.IrasPortal.Application.Constants;
using static Rsp.IrasPortal.Domain.AccessControl.Permissions;

namespace Rsp.IrasPortal.Application.AccessControl;

/// <summary>
/// Maps roles to their workspace, area, and action permissions This can later be moved to
/// RoleClaims table in the database
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, List<string>> _rolePermissionMap = new()
    {
        {
            Roles.Applicant, new List<string>
            {
                MyResearch.Workspace_Access,

                // My Research Workspace
                MyResearch.ProjectRecord_Read,
                MyResearch.ProjectRecord_Create,
                MyResearch.ProjectRecord_Update,
                MyResearch.ProjectRecord_Delete,
                MyResearch.ProjectRecord_List,
                MyResearch.ProjectRecord_Search,
                MyResearch.ProjectRecordHistory_Read,

                MyResearch.ProjectDocuments_Upload,
                MyResearch.ProjectDocuments_Update,
                MyResearch.ProjectDocuments_Review,
                MyResearch.ProjectDocuments_Delete,
                MyResearch.ProjectDocuments_Download,
                MyResearch.ProjectDocuments_List,

                MyResearch.Modifications_Create,
                MyResearch.Modifications_Read,
                MyResearch.Modifications_Update,
                MyResearch.Modifications_Delete,
                MyResearch.Modifications_Review,
                MyResearch.Modifications_List,
                MyResearch.Modifications_Search,
                MyResearch.Modifications_Submit,

                MyResearch.ModificationsHistory_Read
            }
        },
        {
            // sponsor role has access to Sponsor Workspace and My Research Workspace (read-only)
            Roles.Sponsor, new List<string>
            {
                Sponsor.Workspace_Access,

                // My Research Workspace (read-only)
                MyResearch.ProjectRecord_Read,
                MyResearch.ProjectRecordHistory_Read,

                MyResearch.ProjectDocuments_List,
                MyResearch.ProjectDocuments_Download,

                MyResearch.Modifications_List,
                MyResearch.Modifications_Review,
                MyResearch.ModificationsHistory_Read,

                // Sponsor Workspace
                Sponsor.Modifications_Review,
                Sponsor.Modifications_Search,
                Sponsor.Modifications_Authorise,
                Sponsor.MyOrganisations_Access,
                Sponsor.MyOrganisations_Search,
            }
        },
        {
            Roles.WorkflowCoordinator, new List<string>
            {
                Approvals.Workspace_Access,

                // My Research Workspace (read-only)
                MyResearch.ProjectRecord_Read,
                MyResearch.ProjectRecordHistory_Read,

                MyResearch.ProjectDocuments_List,
                MyResearch.ProjectDocuments_Download,

                MyResearch.Modifications_List,
                MyResearch.Modifications_Review,
                MyResearch.ModificationsHistory_Read,
                MyResearch.ModificationsHistory_BackStage_Read,
                MyResearch.Modifications_Approve,

                // Approvals Workspace
                Approvals.ProjectRecords_Search,
                Approvals.ModificationRecords_Search,
                Approvals.Modifications_Assign
            }
        },
        {
            Roles.TeamManager, new List<string>
            {
                Approvals.Workspace_Access,

                //MyRearch Workspace
                MyResearch.ProjectRecord_Read,
                MyResearch.ProjectRecordHistory_Read,

                MyResearch.ProjectDocuments_List,
                MyResearch.ProjectDocuments_Download,

                MyResearch.Modifications_List,
                MyResearch.Modifications_Review,
                MyResearch.ModificationsHistory_Read,
                MyResearch.ModificationsHistory_BackStage_Read,
                MyResearch.Modifications_Approve,

                // Approvals Workspace
                Approvals.ProjectRecords_Search,
                Approvals.ModificationRecords_Search,
                Approvals.Modifications_Assign,
                Approvals.Modifications_Reassign
            }
        },
        {
            Roles.StudyWideReviewer, new List<string>
            {
                Approvals.Workspace_Access,

                //MyRearch Workspace
                MyResearch.ProjectRecord_Read,
                MyResearch.ProjectRecordHistory_Read,

                MyResearch.ProjectDocuments_List,
                MyResearch.ProjectDocuments_Download,

                MyResearch.Modifications_List,
                MyResearch.Modifications_Review,
                MyResearch.ModificationsHistory_Read,
                MyResearch.ModificationsHistory_BackStage_Read,
                MyResearch.Modifications_Approve,

                // Approvals Workspace
                Approvals.ProjectRecords_Search,
                Approvals.ModificationRecords_Search
            }
        },
                {
            // sponsor role has access to Sponsor Workspace and My Research Workspace (read-only)
            Roles.OrganisationAdministrator, new List<string>
            {
                Sponsor.Workspace_Access,

                // My Research Workspace (read-only)
                MyResearch.ProjectRecord_Read,
                MyResearch.ProjectRecordHistory_Read,

                MyResearch.ProjectDocuments_List,
                MyResearch.ProjectDocuments_Download,

                MyResearch.Modifications_List,
                MyResearch.Modifications_Review,
                MyResearch.ModificationsHistory_Read,

                // Sponsor Workspace
                Sponsor.Modifications_Review,
                Sponsor.Modifications_Search,
                Sponsor.Modifications_Authorise,
                Sponsor.MyOrganisations_Access,
                Sponsor.MyOrganisations_Search,
            }
        },
    };

    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    public static List<string> GetPermissionsForRole(string role)
    {
        return _rolePermissionMap.TryGetValue(role, out var permissions)
            ? permissions
            : [];
    }

    /// <summary>
    /// Gets all permissions for multiple roles (union of all role permissions)
    /// </summary>
    public static List<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var allPermissions = new HashSet<string>();

        foreach (var role in roles)
        {
            var permissions = GetPermissionsForRole(role);
            foreach (var permission in permissions)
            {
                allPermissions.Add(permission);
            }
        }

        return [.. allPermissions];
    }
}