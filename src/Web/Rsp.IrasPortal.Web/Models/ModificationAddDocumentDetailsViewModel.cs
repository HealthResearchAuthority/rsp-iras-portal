using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel for capturing details of a document being added during a modification process.
/// </summary>
public class ModificationAddDocumentDetailsViewModel : QuestionnaireViewModel
{
    /// <summary>
    /// Gets or sets the IRAS (Integrated Research Application System) identifier for the project.
    /// </summary>
    public string IrasId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the short title of the project.
    /// </summary>
    public string? ShortTitle { get; set; }

    /// <summary>
    /// Gets or sets the identifier associated with the current project modification.
    /// </summary>
    public string ModificationIdentifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the title displayed on the page for context.
    /// </summary>
    public string? PageTitle { get; set; }

    /// <summary>
    /// Gets or sets the Project Record Id.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

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
}