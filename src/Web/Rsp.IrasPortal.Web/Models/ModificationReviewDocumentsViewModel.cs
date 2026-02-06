using Rsp.Portal.Application.DTOs;

namespace Rsp.Portal.Web.Models;

/// <summary>
/// ViewModel for reviewing documents uploaded during a project modification process.
/// </summary>
public class ModificationReviewDocumentsViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// List of uploaded documents to be reviewed.
    /// </summary>
    public List<DocumentSummaryItemDto> UploadedDocuments { get; set; } = [];
}