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
    public const string RequestForInformation = "Request for further information";
    public const string Withdrawn = "Withdrawn";

    // BACKSTAGE VALUES
    public const string Received = "Received";

    public const string ReviewInProgress = "Review in progress";
    public const string RequestRevisions = "Request revision";
    public const string ReviseAndAuthorise = "Sponsor revises modification";

    public static readonly List<string> Types = [
        InDraft,
        WithSponsor,
        WithReviewBody,
        Approved,
        NotApproved,
        NotAuthorised
    ];

    public static readonly List<string> InTransactionStatus = [
        InDraft,
        WithSponsor,
        RequestRevisions,
        ReviseAndAuthorise
   ];
}