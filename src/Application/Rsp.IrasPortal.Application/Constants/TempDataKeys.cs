namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// TempData Keys. These keys are used
/// to lookup items stored in TempData dictionary.
/// </summary>
public struct TempDataKeys
{
    public const string ApplicationId = "td:application_id";
    public const string PreviousStage = "td:app_previousstage";
    public const string CurrentStage = "td:app_currentstage";
    public const string UploadedDocuments = "td:uploaded_documents";
    public const string VersionId = "td:version_id";
}