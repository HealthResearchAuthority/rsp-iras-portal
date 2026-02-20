using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;

namespace Rsp.Portal.Web.Models;

public class ModificationUploadDocumentsViewModel : BaseProjectModificationViewModel
{
    public List<IFormFile> Files { get; set; } = [];

    /// <summary>
    /// List of uploaded documents to be reviewed.
    /// </summary>
    public List<DocumentSummaryItemDto> UploadedDocuments { get; set; } = [];

    public bool ShouldReturnView { get; set; }
    public bool ShouldRedirectToDocumentsAdded { get; set; }
    public bool HasServiceError { get; set; }
    public ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>? Response { get; set; }
}