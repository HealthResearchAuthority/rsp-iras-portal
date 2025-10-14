namespace Rsp.IrasPortal.Application.Constants;

public struct ProjectRecordStatus
{
    public const string InDraft = "In draft";
    public const string Active = "Active";    
    public static List<string> AllOptions => new() { InDraft, Active };
}