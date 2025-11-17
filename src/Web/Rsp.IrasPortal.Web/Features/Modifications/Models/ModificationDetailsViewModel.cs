using Rsp.IrasPortal.Application.Constants;
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
    public ProjectOverviewDocumentViewModel ProjectOverviewDocumentViewModel { get; set; } = new();

    public string? Outcome { get; set; }
    public IEnumerable<SupportingDocumentModel> SupportingDocuments =>
            ModificationChanges
                .SelectMany(mc => mc.SupportingDocuments ?? Enumerable.Empty<SupportingDocumentModel>())
                .GroupBy(doc => doc.Link)
                .Select(group => group.First());

    public void UpdateOverAllRanking()
    {
        var modificationTypes =
            (from change in ModificationChanges
             where change.ModificationType.Order > 0
             select change.ModificationType).ToList();

        var categories =
            (from change in ModificationChanges
             where change.Categorisation.Order > 0
             select change.Categorisation).ToList();

        var reviewTypes =
            (from change in ModificationChanges
             select change.ReviewType).ToList();

        // overall modification ranking
        ModificationType = modificationTypes.Count != 0 ?
            modificationTypes.MinBy(mt => mt.Order).Substantiality :
            Ranking.NotAvailable;

        // check if have categories B and C but not A, then overall category is B/C
        if (categories.Any(c => c.Category == Ranking.CategoryTypes.B) &&
            categories.Any(c => c.Category == Ranking.CategoryTypes.C) &&
            !categories.Any(c => c.Category == Ranking.CategoryTypes.A))
        {
            Category = Ranking.CategoryTypes.BC;
        }
        else
        {
            Category = categories.Count != 0 ?
            categories.MinBy(mt => mt.Order).Category :
            Ranking.NotAvailable;
        }

        ReviewType = reviewTypes.FirstOrDefault(r => r.Equals(Ranking.ReviewTypes.ReviewRequired)) ??
                     reviewTypes.FirstOrDefault(r => r.Equals(Ranking.ReviewTypes.NoReviewRequired)) ??
                     Ranking.NotAvailable;
    }
}