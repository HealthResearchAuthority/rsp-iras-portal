namespace Rsp.IrasPortal.Application.Constants;

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
        public const string ProjectModificationChangeId = "td:project_modification_change_id";
        public const string AreaOfChanges = "td:areaofchanges";
        public const string AreaOfChangeId = "td:area_of_change_id";
        public const string SpecificAreaOfChangeId = "td:specific_area_of_change_id";
        public const string ShowApplicabilityQuestions = "td:show_applicability_questions";
        public const string AreaOfChangeText = "td:area_of_change_text";
        public const string SpecificAreaOfChangeText = "td:specific_area_of_changeText";
        public const string Questionnaire = "td:modification_questionnaire";
        public const string ProjectModificationChangeMarker = "td:project_modification_change_marker";
        public const string LinkBackToReferrer = "td:link_back_to_referrer";
        public const string UrlReferrer = "td:url_referrer";
        public const string ReviewAllChanges = "td:modification_review_all_changes";
        public const string OverallReviewType = "td:modification_overall_review_type";
        public const string DateCreated = "td:created_date";
        public const string OverallRanking = "td:modification_overall_ranking";
        public const string ProjectModificationsDetails = "td:project_modifications_details";
        public const string ProjectModificationStatus = "td:project_modification_status";
    }

    public struct ProjectModificationChange
    {
        public const string ReviewChanges = "td:modification_review_changes";
        public const string Navigation = "td:modification_change_navigation";
        public const string ChangeRemoved = "td:modification_change_removed";
        public const string ChangeName = "td:modification_change_name";
        public const string RankingOfChange = "td:modification_ranking_of_change";
    }

    public const string ProjectRecordId = "td:project_record_id";
    public const string ProjectRecord = "td:project_record";
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
    public const string ShowProjectDeletedBanner = "td:show_project_deleted_banner";
    public const string OrganisationSearchModel = "td:organisationSearchModel";
    public const string Status = "td:status";
    public const string SelectedProjectModifications = "td:selected_project_modifications";
    public const string ModificationTasklistReviewerId = "td:reviewer_id";
    public const string BackRoute = "td:back_route";
    public const string ShowNoResultsFound = "td:show_no_results_found";
    public const string ShowEditLink = "td:show_edit_link";
    public const string SponsorOrganisationType = "td:sponsor_org_type";
    public const string SponsorOrganisationUserType = "td:sponsor_org_user_type";
    public const string ShowCookiesSavedNotificationBanner = "td:show_cookies_notification_banner";
    public const string ShowCookiesSavedHeaderBanner = "td:show_cookies_header_banner";
    public const string ActiveSponsoOrganisationFilterName = "td:active_sponsor_org_filter";
    public const string ChangeSuccess = "td:change_success";
}