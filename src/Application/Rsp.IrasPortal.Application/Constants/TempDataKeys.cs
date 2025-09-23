﻿namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// TempData Keys. These keys are used
/// to lookup items stored in TempData dictionary.
/// </summary>
public struct TempDataKeys
{
    public struct ProjectModification
    {
        public const string ProjectModificationId = "td:project_modification_id";
        public const string ProjectModificationIdentifier = "td:project_modification_identifier";
        public const string ProjectModificationSpecificArea = "td:project_modification_specific_area";
        public const string ProjectModificationChangeId = "td:project_modification_change_id";
        public const string AreaOfChanges = "td:areaofchanges";
        public const string AreaOfChangeId = "td:area_of_change_id";
        public const string SpecificAreaOfChangeId = "td:specific_area_of_change_id";
        public const string AreaOfChangeText = "td:area_of_change_text";
        public const string SpecificAreaOfChangeText = "td:specific_area_of_changeText";
        public const string Questionnaire = "td:modification_questionnaire";
        public const string ProjectModificationChangeMarker = "td:project_modification_change_marker";
        public const string JourneyType = "td:journey_type";
    }

    public struct ProjectModificationChange
    {
        public const string ReviewChanges = "td:modification_review_changes";
        public const string Navigation = "td:modification_change_navigation";
    }

    public const string ProjectRecordId = "td:project_record_id";
    public const string ProjectRecordResponses = "td:project_record_responses";
    public const string ShortProjectTitle = "td:short_project_title";
    public const string PlannedProjectEndDate = "td:planned_project_enddate";
    public const string CategoryId = "td:category_id";
    public const string IrasId = "td:iras_id";
    public const string PreviousStage = "td:app_previousstage";
    public const string PreviousCategory = "td:app_previouscategory";
    public const string CurrentStage = "td:app_currentstage";
    public const string UploadedDocuments = "td:uploaded_documents";
    public const string VersionId = "td:version_id";
    public const string QuestionSetPublishSuccess = "td:qset_publish_success";
    public const string QuestionSetUploadSuccess = "td:qset_upload_success";
    public const string ProjectOverview = "td:project_overview";
    public const string SponsorOrganisations = "td:sponsor_organisations";
    public const string SponsorOrgSearched = "td:sponsor_org_searched";
    public const string OrgSearch = "td:org_search";
    public const string OrgSearchReturnUrl = "td:org_search_return_url";
    public const string ModelState = "td:model_state";
    public const string ShowNotificationBanner = "td:show_notification_banner";
    public const string OrganisationSearchModel = "td:organisationSearchModel";
    public const string Status = "td:status";
    public const string SelectedProjectModifications = "td:selected_project_modifications";
    public const string ModificationTasklistReviewerId = "td:reviewer_id";
}