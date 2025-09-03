namespace Rsp.IrasPortal.Web.Models;

public class SponsorReferenceViewModel : BaseProjectModificationViewModel
{
    public string? SponsorModificationReference { get; set; }

    public DateViewModel SponsorModificationDate { get; set; } = new DateViewModel();

    public string? MainChangesDescription { get; set; }
}