using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications.Models;

public class ModificationDetailsViewModel : BaseProjectModificationViewModel
{
    public new string Status { get; set; } = null!;
    public new string ReasonNotApproved { get; set; } = null!;
    public new string ReviewerComments { get; set; } = null!;
    public string? ModificationType { get; set; }
    public string? Category { get; set; }
    public string? ReviewType { get; set; }
    public List<ModificationChangeModel> ModificationChanges { get; set; } = [];
    public bool ChangesReadyForSubmission { get; set; }
    public bool NoChangesToSubmit { get; set; }
    public List<QuestionViewModel> SponsorDetails { get; set; } = [];
    public ProjectOverviewDocumentViewModel ProjectOverviewDocumentViewModel { get; set; } = new();
    public AuditTrailModel? AuditTrailModel { get; set; }

    public string? Outcome { get; set; }

    public IEnumerable<SupportingDocumentModel> SupportingDocuments =>
            ModificationChanges
                .SelectMany(mc => mc.SupportingDocuments ?? Enumerable.Empty<SupportingDocumentModel>())
                .GroupBy(doc => doc.Link)
                .Select(group => group.First());
}