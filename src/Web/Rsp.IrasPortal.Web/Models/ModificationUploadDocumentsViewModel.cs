using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

public class ModificationUploadDocumentsViewModel : BaseProjectModificationViewModel
{
    public List<IFormFile> Files { get; set; } = [];

    /// <summary>
    /// List of uploaded documents to be reviewed.
    /// </summary>
    public List<DocumentSummaryItemDto> UploadedDocuments { get; set; } = [];
}