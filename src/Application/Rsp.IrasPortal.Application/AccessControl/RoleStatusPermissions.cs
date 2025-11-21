using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Application.AccessControl;

/// <summary>
/// Maps roles to their allowed record statuses for different entity types
/// </summary>
public static class RoleStatusPermissions
{
    /// <summary>
    /// Defines which statuses a role can access for Project Records
    /// </summary>
    public static class ProjectRecord
    {
        private static readonly Dictionary<string, List<string>> _roleStatusMap = new()
        {
            {
                Roles.Applicant, new List<string>
                {
                    ProjectRecordStatus.InDraft,
                    ProjectRecordStatus.Active
                }
            },
            {
                Roles.Sponsor, new List<string>
                {
                    ProjectRecordStatus.Active
                }
            },
            {
                Roles.SystemAdministrator, new List<string>
                {
                    ProjectRecordStatus.InDraft,
                    ProjectRecordStatus.Active
                }
            },
            {
                Roles.WorkflowCoordinator, new List<string>
                {
                    ProjectRecordStatus.Active
                }
            },
            {
                Roles.TeamManager, new List<string>
                {
                    ProjectRecordStatus.Active
                }
            },
            {
                Roles.StudyWideReviewer, new List<string>
                {
                    ProjectRecordStatus.Active
                }
            }
        };

        /// <summary>
        /// Gets the statuses that a user with specific roles can access
        /// </summary>
        public static List<string> GetAllowedStatuses(IEnumerable<string> userRoles)
        {
            var allowedStatuses = new HashSet<string>();

            foreach (var role in userRoles)
            {
                if (_roleStatusMap.TryGetValue(role, out var statuses))
                {
                    foreach (var status in statuses)
                    {
                        allowedStatuses.Add(status);
                    }
                }
            }

            return [.. allowedStatuses];
        }
    }

    /// <summary>
    /// Defines which statuses a role can access for Modifications
    /// </summary>
    public static class Modification
    {
        public static readonly Dictionary<string, List<string>> _roleStatusMap = new()
        {
            {
                Roles.Applicant, new List<string>
                {
                    ModificationStatus.InDraft,
                    ModificationStatus.WithSponsor,
                    ModificationStatus.WithReviewBody,
                    ModificationStatus.Approved,
                    ModificationStatus.NotAuthorised,
                    ModificationStatus.NotApproved
                }
            },
            {
                Roles.Sponsor, new List<string>
                {
                    ModificationStatus.WithSponsor,
                    ModificationStatus.WithReviewBody,
                    ModificationStatus.Approved,
                    ModificationStatus.NotAuthorised,
                    ModificationStatus.NotApproved
                }
            },
            {
                Roles.WorkflowCoordinator, new List<string>
                {
                    ModificationStatus.WithReviewBody,
                    ModificationStatus.Approved,
                    ModificationStatus.NotApproved
                }
            },
            {
                Roles.TeamManager, new List<string>
                {
                    ModificationStatus.WithReviewBody,
                    ModificationStatus.Approved,
                    ModificationStatus.NotApproved
                }
            },
            {
                Roles.StudyWideReviewer, new List<string>
                {
                    ModificationStatus.WithReviewBody,
                    ModificationStatus.Approved,
                    ModificationStatus.NotApproved
                }
            }
        };

        /// <summary>
        /// Gets the statuses that a user with specific roles can access
        /// </summary>
        public static List<string> GetAllowedStatuses(IEnumerable<string> userRoles)
        {
            var allowedStatuses = new HashSet<string>();

            foreach (var role in userRoles)
            {
                if (_roleStatusMap.TryGetValue(role, out var statuses))
                {
                    foreach (var status in statuses)
                    {
                        allowedStatuses.Add(status);
                    }
                }
            }

            return [.. allowedStatuses];
        }
    }

    /// <summary>
    /// Defines which statuses a role can access for Documents
    /// </summary>
    public static class Document
    {
        public static readonly Dictionary<string, List<string>> RoleStatusAccess = new()
        {
            {
                Roles.Applicant, new List<string>
                {
                    DocumentStatus.Uploaded,
                    DocumentStatus.Failed,
                    DocumentStatus.Incomplete,
                    DocumentStatus.Complete,
                    DocumentStatus.WithSponsor,
                    DocumentStatus.WithReviewBody,
                    DocumentStatus.Approved,
                    DocumentStatus.NotAuthorised,
                    DocumentStatus.NotApproved
                }
            },
            {
                Roles.Sponsor, new List<string>
                {
                    DocumentStatus.WithReviewBody,
                    DocumentStatus.Approved,
                    DocumentStatus.NotApproved
                }
            },
            {
                Roles.WorkflowCoordinator, new List<string>
                {
                    DocumentStatus.WithReviewBody,
                    DocumentStatus.Approved,
                    DocumentStatus.NotApproved
                }
            },
            {
                Roles.TeamManager, new List<string>
                {
                    DocumentStatus.WithReviewBody,
                    DocumentStatus.Approved,
                    DocumentStatus.NotApproved
                }
            },
            {
                Roles.StudyWideReviewer, new List<string>
                {
                    DocumentStatus.WithReviewBody,
                    DocumentStatus.Approved,
                    DocumentStatus.NotApproved
                }
            }
        };

        /// <summary>
        /// Gets the statuses that a user with specific roles can access
        /// </summary>
        public static List<string> GetAllowedStatuses(IEnumerable<string> userRoles)
        {
            var allowedStatuses = new HashSet<string>();

            foreach (var role in userRoles)
            {
                if (RoleStatusAccess.TryGetValue(role, out var statuses))
                {
                    foreach (var status in statuses)
                    {
                        allowedStatuses.Add(status);
                    }
                }
            }

            return [.. allowedStatuses];
        }
    }

    public static List<string> GetAllowedStatusesForRoles(List<string> userRoles, string entityType)
    {
        return entityType.ToLowerInvariant() switch
        {
            "projectrecord" => ProjectRecord.GetAllowedStatuses(userRoles),
            "modification" => Modification.GetAllowedStatuses(userRoles),
            "document" => Document.GetAllowedStatuses(userRoles),
            _ => []
        };
    }

    public static Dictionary<string, List<string>> GetAllowedStatusesForRoles(IEnumerable<string> userRoles)
    {
        return new Dictionary<string, List<string>>()
        {
            { "projectrecord", ProjectRecord.GetAllowedStatuses(userRoles)  },
            { "modification", Modification.GetAllowedStatuses(userRoles) },
            { "document", Document.GetAllowedStatuses(userRoles) }
        };
    }
}