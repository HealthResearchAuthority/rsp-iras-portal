namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// HttpContext Items Keys. The keys are used
/// to lookup HttpContext.Items
/// </summary>
public struct ContextItemKeys
{
    public const string BearerToken = "bearer_token";
    public const string AcessToken = "access_token";
    public const string IdToken = "id_token";
    public const string RespondentId = "context:respondentid";
    public const string Email = "context:email";
    public const string FirstName = "context:firstname";
    public const string LastName = "context:lastname";
    public const string LastLogin = "context:lastlogin";
}