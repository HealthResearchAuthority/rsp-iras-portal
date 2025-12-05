namespace Rsp.IrasPortal.Application.Constants;

public struct DocumentStatus
{
    public const string Uploaded = "Uploaded";
    public const string Failed = "Failed";
    public const string Incomplete = "Incomplete";
    public const string Complete = "Complete";
    public const string WithSponsor = "With sponsor";
    public const string WithReviewBody = "With review body";
    public const string Approved = "Approved";
    public const string NotAuthorised = "Not authorised";
    public const string NotApproved = "Not approved";

    // BACKSTAGE VALUES
    public const string Received = "Received";

    public const string ReviewInProgress = "Review in progress";
}