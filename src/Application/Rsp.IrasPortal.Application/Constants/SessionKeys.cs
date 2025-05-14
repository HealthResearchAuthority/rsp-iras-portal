namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// Session Keys. These keys are used
/// to lookup items stored in HttpContext.Session
/// </summary>
public static class SessionKeys
{
    public const string Application = "session:application";
    public const string Questionnaire = "session:questionnaire";
    public const string DocumentUpload = "session:document_upload";
    public const string FirstLogin = "session:first_login";
}