namespace Rsp.IrasPortal.Web.Models;

public class ModificationDetailsPageViewModel : BaseProjectModificationViewModel
{
    public Guid ModificationId { get; set; }
    public string Status { get; set; } = null!;
    public string? ModificationType { get; set; }
    public string? Category { get; set; }
    public string? ReviewType { get; set; }
    public IEnumerable<ModificationChangeModel> ModificationChanges { get; set; } = new List<ModificationChangeModel>();
    public bool ChangesReadyForSubmission { get; set; } = false;
    public SponsorReferenceViewModel? SponsorReference { get; set; }
}