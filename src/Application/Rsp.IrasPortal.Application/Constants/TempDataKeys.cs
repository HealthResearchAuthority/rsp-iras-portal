﻿namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// TempData Keys. These keys are used
/// to lookup items stored in TempData dictionary.
/// </summary>
public struct TempDataKeys
{
    public const string ProjectApplicationId = "td:project_application_id";
    public const string ShortProjectTitle = "td:short_project_title";
    public const string CategoryId = "td:category_id";
    public const string IrasId = "td:iras_id";
    public const string PreviousStage = "td:app_previousstage";
    public const string PreviousCategory = "td:app_previouscategory";
    public const string CurrentStage = "td:app_currentstage";
    public const string UploadedDocuments = "td:uploaded_documents";
    public const string VersionId = "td:version_id";
    public const string QuestionSetPublishSuccess = "td:qset_publish_success";
    public const string QuestionSetUploadSuccess = "td:qset_upload_success";
    public const string QuestionSetPublishedVersionId = "td:qset_published_version_id";
    public const string ProjectOverview = "td:project_overview";
    public const string SponsorOrganisations = "td:sponsor_organisations";
    public const string SponsorOrgSearched = "td:sponsor_org_searched";
    public const string OrgSearch = "td:org_search";
    public const string OrgSearchReturnUrl = "td:org_search_return_url";
    public const string ModelState = "td:model_state";
}