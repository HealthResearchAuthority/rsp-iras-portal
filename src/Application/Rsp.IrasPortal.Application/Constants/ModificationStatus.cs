namespace Rsp.IrasPortal.Application.Constants;

public struct ModificationStatus
{
    public const string InDraft = "In draft";
    public const string ChangeReadyForSubmission = "Change ready for submission";
    public const string Unfinished = "Unfinished";
    public const string WithSponsor = "With sponsor";
    public const string WithReviewBody = "With review body";
    public const string Approved = "Approved";
    public const string NotApproved = "Not approved";

    public static readonly List<string> Types = [
        InDraft,
        WithSponsor,
        WithReviewBody,
        Approved,
        NotApproved
    ];
}