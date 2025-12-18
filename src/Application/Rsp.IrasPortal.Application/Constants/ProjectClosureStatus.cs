namespace Rsp.IrasPortal.Application.Constants;

public struct ProjectClosureStatus
{
    public const string WithSponsor = "With sponsor";
    public const string Authorised = "Authorised";
    public const string NotAuthorised = "Not authorised";

    public static readonly List<string> Types = [
        WithSponsor,
        Authorised,
        NotAuthorised
    ];
}