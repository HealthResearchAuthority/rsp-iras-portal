using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Models;

public class ModificationDetailsViewModel : BaseProjectModificationViewModel
{
    public new string Status { get; set; } = null!;
    public string? ModificationType { get; set; }
    public string? Category { get; set; }
    public string? ReviewType { get; set; }
    public List<ModificationChangeModel> ModificationChanges { get; set; } = [];
    public bool ChangesReadyForSubmission { get; set; }
    public List<QuestionViewModel> SponsorDetails { get; set; } = [];

    public IEnumerable<SupportingDocumentModel> SupportingDocuments =>
            ModificationChanges
                .SelectMany(mc => mc.SupportingDocuments ?? Enumerable.Empty<SupportingDocumentModel>())
                .GroupBy(doc => doc.Link)
                .Select(group => group.First());

    public void UpdateOverAllRanking()
    {
        var modificationTypes =
            from change in ModificationChanges
            select change.ModificationType;

        var categories =
            from change in ModificationChanges
            select change.Categorisation;

        var reviewTypes =
            from change in ModificationChanges
            select change.ReviewType;

        // overall modification ranking
        ModificationType = modificationTypes.Any() ?
            modificationTypes.MinBy(mt => mt.Order).Substantiality :
            "Not available";

        Category = categories.Any() ?
            categories.MinBy(mt => mt.Order).Category :
            "N/A";

        ReviewType = reviewTypes.FirstOrDefault(r => r.Equals("Review required")) ??
                     reviewTypes.FirstOrDefault(r => r.Equals("No review required")) ??
                     "Not available";
    }
}