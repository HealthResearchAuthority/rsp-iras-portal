namespace Rsp.Portal.Application.Constants;

public struct ModificationStatus
{
    public const string InDraft = "In draft";
    public const string ChangeReadyForSubmission = "Change ready for submission";
    public const string Unfinished = "Unfinished";
    public const string WithSponsor = "With sponsor";
    public const string WithReviewBody = "With review body";
    public const string Approved = "Approved";
    public const string NotApproved = "Not approved";
    public const string NotAuthorised = "Not authorised";

    // BACKSTAGE VALUES
    public const string Received = "Received";

    public const string ReviewInProgress = "Review in progress";
    public const string RequestRevisions = "Request revisions";
    public const string ReviseAndAuthorise = "Revise and authorise";

    public static readonly List<string> Types = [
        InDraft,
        WithSponsor,
        WithReviewBody,
        Approved,
        NotApproved,
        NotAuthorised
    ];
}