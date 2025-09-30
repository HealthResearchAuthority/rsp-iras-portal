namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// ViewData Keys. These keys are used
/// to lookup items stored in ViewData dictionary.
/// </summary>
public struct ViewDataKeys
{
    public const string IsQuestionnaireValid = "vd:is_questionnaire_valid";
    public const string IsApplicationValid = "vd:is_application_valid";
    public const string ConditionalClass = "vd:conditional_class";
    public const string ShowModificationStatus = "vd:show_modification_status";
    public const string ShowModificationChangeStatus = "vd:show_modification_change_status";
    public const string ShowChangeLink = "vd:show_change_link";
    public const string ShowRemoveLink = "vd:show_remove_link";
    public const string UrlReferrer = "vd:url_referrer";
}