using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel for capturing details of a document being added during a modification process.
/// </summary>
public class ModificationAddDocumentDetailsViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// Unique identifier for the uploaded document.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Name of the uploaded file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Size of the uploaded file (formatted as a string, e.g., "1.2 MB").
    /// </summary>
    public string FileSize { get; set; } = string.Empty;

    /// <summary>
    /// Selected document type ID.
    /// </summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Dropdown options for selecting the document type.
    /// </summary>
    public List<SelectListItem> DocumentTypeOptions { get; set; } = [];

    /// <summary>
    /// Path to where the document is stored (e.g., in blob storage or file system).
    /// </summary>
    public string? DocumentStoragePath { get; set; }

    /// <summary>
    /// Indicates if the answers are being reviewed.
    /// </summary>
    public bool ReviewAnswers { get; set; }

    /// <summary>
    /// Questionnaire view model containing dynamic questions/answers.
    /// </summary>
    public QuestionnaireViewModel Questionnaire { get; set; } = new();
}