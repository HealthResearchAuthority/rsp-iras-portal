namespace Rsp.IrasPortal.Application.Constants;

public struct ModificationStatus
{
    public const string InDraft = "In draft";
    public const string ChangeReadyForSubmission = "Change ready for submission";
    public const string Unfinished = "Unfinished";
    public const string WithSponsor = "With sponsor";
    public const string WithRegulator = "With regulator";
    public const string Approved = "Approved";
    public const string NotApproved = "Not approved";
    public const string Authorised = "Authorised";
    public const string NotAuthorised = "Not authorised";

    public static readonly List<string> Types = [
        InDraft,
        WithSponsor,
        WithRegulator,
        Approved,
        NotApproved,
        Authorised,
        NotAuthorised
    ];
}