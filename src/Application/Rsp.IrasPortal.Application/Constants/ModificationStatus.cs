namespace Rsp.IrasPortal.Application.Constants;

public struct ModificationStatus
{
    public const string ModificationRecordStarted = "Draft";
    public const string ChangeReadyForSubmission = "Change ready for submission";
    public const string Unfinished = "Unfinished";
    public const string ModificationSubmittedBySponsor = "In sponsor review";
    public const string Approved = "Approved";
    public const string NotApproved = "Not approved";
}