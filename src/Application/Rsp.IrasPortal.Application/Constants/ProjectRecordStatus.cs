namespace Rsp.IrasPortal.Application.Constants;

public struct ProjectRecordStatus
{
    public const string InDraft = "In draft";
    public const string Active = "Active";
    public const string PendingClosure = "Pending closure";
    public const string Closed = "Closed";
    public static List<string> AllOptions => [InDraft, Active, PendingClosure, Closed];
}