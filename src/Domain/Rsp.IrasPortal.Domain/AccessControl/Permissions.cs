namespace Rsp.IrasPortal.Domain.AccessControl;

/// <summary>
/// Defines all workspace.area.action permissions in the system
/// </summary>
public static class Permissions
{
    // My Research Workspace Permissions
    public static class MyResearch
    {
        public const string Workspace_Access = "workspace.myresearch";

        public const string ProjectRecord_Create = "myresearch.projectrecord.create";
        public const string ProjectRecord_Read = "myresearch.projectrecord.read";
        public const string ProjectRecord_Update = "myresearch.projectrecord.update";
        public const string ProjectRecord_Delete = "myresearch.projectrecord.delete";
        public const string ProjectRecord_Search = "myresearch.projectrecord.search";
        public const string ProjectRecordHistory_Read = "myresearch.projectrecordhistory.read";

        public const string ProjectDocuments_Read = "myresearch.projectdocuments.read";
        public const string ProjectDocuments_Update = "myresearch.projectdocuments.update";
        public const string ProjectDocuments_Upload = "myresearch.projectdocuments.upload";
        public const string ProjectDocuments_Download = "myresearch.projectdocuments.download";
        public const string ProjectDocuments_Delete = "myresearch.projectdocuments.delete";

        public const string Modifications_Create = "myresearch.modifications.create";
        public const string Modifications_Read = "myresearch.modifications.read";
        public const string Modifications_Update = "myresearch.modifications.update";
        public const string Modifications_Delete = "myresearch.modifications.delete";
        public const string Modifications_Search = "myresearch.modifications.search";
        public const string Modifications_Review = "myresearch.modifications.review";
        public const string Modifications_Submit = "myresearch.modifications.submit";

        public const string ModificationsHistory_Read = "myresearch.modificationshistory.read";
    }

    // Sponsor Workspace Permissions
    public static class Sponsor
    {
        // this permission to represent the whole workspace
        // and should only be mapped to roles that have full access to this workspace
        public const string Workspace_Access = "workspace.sponsor";

        public const string Modifications_Search = "sponsor.modifications.search";
        public const string Modifications_Review = "sponsor.modifications.review";
        public const string Modifications_Authorise = "sponsor.modifications.authorise";
    }

    // System Administration Workspace Permissions
    public static class SystemAdministration
    {
        public const string Workspace_Access = "workspace.systemadmin";
    }

    // Approvals Workspace Permissions
    public static class Approvals
    {
        public const string Workspace_Access = "workspace.approvals";

        public const string ProjectRecords_Search = "approvals.projectrecords.search";
        public const string ModificationRecords_Search = "approvals.modificationrecords.search";

        public const string Modifications_Assign = "approvals.modifications.assign";
        public const string Modifications_ReAssign = "approvals.modifications.reassign";

        public const string Modifications_Read = "approvals.modifications.read";
        public const string Modifications_Review = "approvals.modifications.review";
        public const string Modifications_Approve = "approvals.modifications.approve";
        public const string Modifications_Update = "approvals.modifications.update";
    }

    // Cag Members Workspace Permissions
    public static class CAGMembers
    {
        public const string Workspace_Access = "workspace.cagmembers";
    }

    // Member Management Workspace Permissions
    public static class MemberManagement
    {
        public const string Workspace_Access = "workspace.membermanagement";
    }

    // CAT Workspace Permissions
    public static class CAT
    {
        public const string Workspace_Access = "workspace.cat";
    }

    // REC Members Workspace Permissions
    public static class RECMembers
    {
        public const string Workspace_Access = "workspace.recmembers";
    }

    // Technical Assurance Workspace Permissions
    public static class TechnicalAssurance
    {
        public const string Workspace_Access = "workspace.technicalassurance";
    }

    // Technical Assurance Reviewers Workspace Permissions
    public static class TechnicalAssuranceReviewers
    {
        public const string Workspace_Access = "workspace.technicalassurancereviewers";
    }
}