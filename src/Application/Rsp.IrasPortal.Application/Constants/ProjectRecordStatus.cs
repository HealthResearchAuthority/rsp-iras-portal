namespace Rsp.Portal.Application.Constants;

public struct ProjectRecordStatus
{
    public const string InDraft = "In draft";
    public const string Active = "Active";
    public const string PendingClosure = "Pending closure";
    public const string Closed = "Closed";
    public const string ProjectHalt = "Project halt";
    public const string ProjectRestart = "Project restart";
    public static List<string> AllOptions => [InDraft, Active, PendingClosure, Closed, ProjectHalt, ProjectRestart];
}