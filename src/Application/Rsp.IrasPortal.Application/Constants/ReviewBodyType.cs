namespace Rsp.IrasPortal.Application.Constants;

public static class ReviewBodyType
{
    public const string ResearchEthicsCommittee = "Research ethics committee";
    public const string StudyWideReview = "Study-wide review";

    public static readonly IReadOnlyDictionary<string, string> OptionsMap =
        new Dictionary<string, string>
        {
            [nameof(ResearchEthicsCommittee)] = ReviewBodyType.ResearchEthicsCommittee,
            [nameof(StudyWideReview)] = ReviewBodyType.StudyWideReview
        };
}