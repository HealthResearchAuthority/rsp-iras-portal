namespace Rsp.IrasPortal.Web.Models;

public class ModificationReviewDocumentsViewModel : BaseProjectModificationViewModel
{
    public List<DocumentSummaryItem> UploadedDocuments { get; set; } = [];
}