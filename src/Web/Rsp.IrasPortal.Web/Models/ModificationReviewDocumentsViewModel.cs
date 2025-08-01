using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

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