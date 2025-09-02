namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// Session Keys. These keys are used
/// to lookup items stored in HttpContext.Session
/// </summary>
public static class SessionKeys
{
    public const string ProjectRecord = "session:project_record";
    public const string Questionnaire = "session:questionnaire";
    public const string DocumentUpload = "session:document_upload";
    public const string FirstLogin = "session:first_login";
    public const string Alive = "session:alive";
    public const string ApprovalsSearch = "session:approvalssearch";
    public const string UsersSearch = "session:userssearch";
    public const string ReviewBodiesSearch = "session:reviewbodiessearch";
    public const string ModificationsTasklist = "session:modificationstasklist";
    public const string MyTasklist = "session:mytasklist";
}