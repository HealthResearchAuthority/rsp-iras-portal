namespace Rsp.Portal.Domain.AccessControl;

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
        /// allows reading project record details
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
        /// allows viewing list of project records
        /// </summary>
        public const string ProjectRecord_List = "myresearch.projectrecord.list";

        /// <summary>
        /// allows searching project records
        /// </summary>
        public const string ProjectRecord_Search = "myresearch.projectrecord.search";

        /// <summary>
        /// allows searching project close
        /// </summary>
        public const string ProjectRecord_Close = "myresearch.projectrecord.close";

        /// <summary>
        /// allows reading project record history
        /// </summary>
        public const string ProjectRecordHistory_Read = "myresearch.projectrecordhistory.read";

        /// <summary>
        /// allows viewing the list of project documents
        /// </summary>
        public const string ProjectDocuments_List = "myresearch.projectdocuments.list";

        /// <summary>
        /// allows reviewing the details of project documents before the final action
        /// </summary>
        public const string ProjectDocuments_Review = "myresearch.projectdocuments.review";

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
        /// allows reading modification details
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
        /// allows viewing list of modifications
        /// </summary>
        public const string Modifications_List = "myresearch.modifications.list";

        /// <summary>
        /// allows searching modifications
        /// </summary>
        public const string Modifications_Search = "myresearch.modifications.search";

        /// <summary>
        /// allows reviewing modifications before the final action i.e. sending to sponsor, approving
        /// </summary>
        public const string Modifications_Review = "myresearch.modifications.review";

        /// <summary>
        /// allows approving modifications
        /// </summary>
        public const string Modifications_Approve = "myresearch.modifications.approve";

        /// <summary>
        /// allows submitting modifications for approval
        /// </summary>
        public const string Modifications_Submit = "myresearch.modifications.submit";

        /// <summary>
        /// allows reading modifications history
        /// </summary>
        public const string ModificationsHistory_Read = "myresearch.modificationshistory.read";

        /// <summary>
        /// allows reading modifications backstage history
        /// </summary>
        public const string ModificationsHistory_BackStage_Read = "myresearch.modificationshistory_backstage.read";

        /// <summary>
        /// allows withdrawing modifications
        /// </summary>
        public const string Modifications_Withdraw = "myresearch.modifications.withdraw";
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
        /// allows reviewing modifications before the final action i.e. authorising, not authorising
        /// </summary>
        public const string Modifications_Review = "sponsor.modifications.review";

        /// <summary>
        /// allows authorising/not authorising modifications
        /// </summary>
        public const string Modifications_Authorise = "sponsor.modifications.authorise";

        /// <summary>
        /// allows searching project closure records
        /// </summary>
        public const string ProjectClosures_Search = "sponsor.projectclosures.search";

        /// <summary>
        /// allows reviewing project closure records before the final action i.e. authorising, not authorising
        /// </summary>
        public const string ProjectClosures_Review = "sponsor.projectclosures.review";

        /// <summary>
        /// allows authorising/not authorising project closure records
        /// </summary>
        public const string ProjectClosures_Authorise = "sponsor.projectclosures.authorise";

        /// <summary>
        /// allows seeing the my organisations on the menu
        /// </summary>
        public const string MyOrganisations_Access = "sponsor.myorganisations.access";

        /// <summary>
        /// allows seeing the my organisations on the menu
        /// </summary>
        public const string MyOrganisations_Search = "sponsor.myorganisations.search";

        /// <summary>
        /// allows seeing the my organisations on the menu
        /// </summary>
        public const string MyOrganisations_Profile = "sponsor.myorganisations.profile";

        /// <summary>
        /// allows seeing the my organisations on the menu
        /// </summary>
        public const string MyOrganisations_Projects = "sponsor.myorganisations.projects";

        /// <summary>
        /// allows seeing the my organisations on the menu
        /// </summary>
        public const string MyOrganisations_Users = "sponsor.myorganisations.users";

        /// <summary>
        /// allows seeing the my organisations on the menu
        /// </summary>
        public const string MyOrganisations_Audit = "sponsor.myorganisations.audit";
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
        /// allows reassigning modifications for review
        /// </summary>
        public const string Modifications_Reassign = "approvals.modifications.reassign";

        /// <summary>
        /// allows reading modifications
        /// </summary>
        public const string Modifications_Read = "approvals.modifications.read";
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