using Rsp.Portal.Application.DTOs;

namespace Rsp.Portal.Web.Models;

public class ModificationUploadDocumentsViewModel : BaseProjectModificationViewModel
{
    public List<IFormFile> Files { get; set; } = [];

    /// <summary>
    /// List of uploaded documents to be reviewed.
    /// </summary>
    public List<DocumentSummaryItemDto> UploadedDocuments { get; set; } = [];
}