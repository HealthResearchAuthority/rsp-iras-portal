namespace Rsp.IrasPortal.Domain.AccessControl;

/// <summary>
/// Defines all workspace.area.action permissions in the system
/// </summary>
public static class Permissions
{
    // My Research Workspace Permissions
    public static class MyResearch
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "myresearch.workspace.access";

        /// <summary>
        /// allows creating new project records
        /// </summary>
        public const string ProjectRecord_Create = "myresearch.projectrecord.create";

        /// <summary>
        /// allows reading project records
        /// </summary>
        public const string ProjectRecord_Read = "myresearch.projectrecord.read";

        /// <summary>
        /// allows updating project records
        /// </summary>
        public const string ProjectRecord_Update = "myresearch.projectrecord.update";

        /// <summary>
        /// allows deleting project records
        /// </summary>
        public const string ProjectRecord_Delete = "myresearch.projectrecord.delete";

        /// <summary>
        /// allows searching project records
        /// </summary>
        public const string ProjectRecord_Search = "myresearch.projectrecord.search";

        /// <summary>
        /// allows reading project record history
        /// </summary>
        public const string ProjectRecordHistory_Read = "myresearch.projectrecordhistory.read";

        /// <summary>
        /// allows viewing the list of project documents
        /// </summary>
        public const string ProjectDocuments_Read = "myresearch.projectdocuments.read";

        /// <summary>
        /// allows adding/updating documents metadata
        /// </summary>
        public const string ProjectDocuments_Update = "myresearch.projectdocuments.update";

        /// <summary>
        /// allows uploading project documents
        /// </summary>
        public const string ProjectDocuments_Upload = "myresearch.projectdocuments.upload";

        /// <summary>
        /// allows downloading project documents
        /// </summary>
        public const string ProjectDocuments_Download = "myresearch.projectdocuments.download";

        /// <summary>
        /// allows deleting project documents
        /// </summary>
        public const string ProjectDocuments_Delete = "myresearch.projectdocuments.delete";

        /// <summary>
        /// allows creating modifications
        /// </summary>
        public const string Modifications_Create = "myresearch.modifications.create";

        /// <summary>
        /// allows reading modifications
        /// </summary>
        public const string Modifications_Read = "myresearch.modifications.read";

        /// <summary>
        /// allows updating modifications
        /// </summary>
        public const string Modifications_Update = "myresearch.modifications.update";

        /// <summary>
        /// allows deleting modifications
        /// </summary>
        public const string Modifications_Delete = "myresearch.modifications.delete";

        /// <summary>
        /// allows searching modifications
        /// </summary>
        public const string Modifications_Search = "myresearch.modifications.search";

        /// <summary>
        /// allows reviwing modifications
        /// </summary>
        public const string Modifications_Review = "myresearch.modifications.review";

        /// <summary>
        /// allows submitting modifications
        /// </summary>
        public const string Modifications_Submit = "myresearch.modifications.submit";

        /// <summary>
        /// allows reading modifications history
        /// </summary>
        public const string ModificationsHistory_Read = "myresearch.modificationshistory.read";
    }

    // Sponsor Workspace Permissions
    public static class Sponsor
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "sponsor.workspace.access";

        /// <summary>
        /// allows searching modifications
        /// </summary>
        public const string Modifications_Search = "sponsor.modifications.search";

        /// <summary>
        /// allows reviwing modifications
        /// </summary>
        public const string Modifications_Review = "sponsor.modifications.review";

        /// <summary>
        /// allows authorising modifications
        /// </summary>
        public const string Modifications_Authorise = "sponsor.modifications.authorise";
    }

    // System Administration Workspace Permissions
    public static class SystemAdministration
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "systemadmin.workspace.access";
    }

    // Approvals Workspace Permissions
    public static class Approvals
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "approvals.workspace.access";

        /// <summary>
        /// allows searching project records
        /// </summary>
        public const string ProjectRecords_Search = "approvals.projectrecords.search";

        /// <summary>
        /// allows searching modification records
        /// </summary>
        public const string ModificationRecords_Search = "approvals.modificationrecords.search";

        /// <summary>
        /// allows assigning modifications for review
        /// </summary>
        public const string Modifications_Assign = "approvals.modifications.assign";

        /// <summary>
        /// allows re-assigning modifications for review
        /// </summary>
        public const string Modifications_ReAssign = "approvals.modifications.reassign";

        /// <summary>
        /// allows reading modifications
        /// </summary>
        public const string Modifications_Read = "approvals.modifications.read";

        /// <summary>
        /// allows reviewing modifications
        /// </summary>
        public const string Modifications_Review = "approvals.modifications.review";

        /// <summary>
        /// allows approving modifications
        /// </summary>
        public const string Modifications_Approve = "approvals.modifications.approve";

        /// <summary>
        /// allows updating modifications e.g. to add review comments
        /// </summary>
        public const string Modifications_Update = "approvals.modifications.update";
    }

    // Cag Members Workspace Permissions
    public static class CAGMembers
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "cagmembers.workspace.access";
    }

    // Member Management Workspace Permissions
    public static class MemberManagement
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "membermanagement.workspace.access";
    }

    // CAT Workspace Permissions
    public static class CAT
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "cat.workspace.access";
    }

    // REC Members Workspace Permissions
    public static class RECMembers
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "recmembers.workspace.access";
    }

    // Technical Assurance Workspace Permissions
    public static class TechnicalAssurance
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "technicalassurance.workspace.access";
    }

    // Technical Assurance Reviewers Workspace Permissions
    public static class TechnicalAssuranceReviewers
    {
        /// <summary>
        /// allows seeing the workspace on the dashboard
        /// </summary>
        public const string Workspace_Access = "technicalassurance.workspace.access";
    }
}