namespace Rsp.IrasPortal.Application.Constants;

public static class ReviewBodyType
{
    public const string ResearchEthicsCommittee = "Research Ethics Committee";
    public const string StudyWideReview = "Study Wide Review";

    public static readonly IReadOnlyDictionary<string, string> OptionsMap =
        new Dictionary<string, string>
        {
            [nameof(ResearchEthicsCommittee)] = ReviewBodyType.ResearchEthicsCommittee,
            [nameof(StudyWideReview)] = ReviewBodyType.StudyWideReview
        };
}