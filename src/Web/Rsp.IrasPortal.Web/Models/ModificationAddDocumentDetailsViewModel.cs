using Rsp.Gds.Component.Models;

namespace Rsp.Portal.Web.Models;

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
    /// Gets or sets the identifier associated with the current modification.
    /// </summary>
    public Guid ModificationId { get; set; }

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
    /// Gets or sets the unique identifier for the modification.
    /// </summary>
    public Guid? Id { get; set; }

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
    public long FileSize { get; set; }

    /// <summary>
    /// Path to where the document is stored (e.g., in blob storage or file system).
    /// </summary>
    public string? DocumentStoragePath { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets if the scan is successful or not.
    /// </summary>
    public bool? IsMalwareScanSuccessful { get; set; }

    /// <summary>
    /// The current document replaces the document identified by this Id.
    /// </summary>
    public string? MetaDataDocumentTypeId { get; set; }

    /// <summary>
    /// The current document replaces the document identified by this Id.
    /// </summary>
    public Guid? ReplacesDocumentId { get; set; }

    /// <summary>
    /// Name of the file to replace.
    /// </summary>
    public string DocumentToReplaceFileName { get; set; } = string.Empty;

    /// <summary>
    /// Path to where the document is stored (e.g., in blob storage or file system).
    /// </summary>
    public string? DocumentToReplaceStoragePath { get; set; }

    /// <summary>
    /// The current document is replaced by the document identified by this Id
    /// </summary>
    public Guid? ReplacedByDocumentId { get; set; }

    /// <summary>
    /// This field will indicate whether the document is CLEAN or TRACKED
    /// </summary>
    public string? DocumentType { get; set; }

    /// <summary>
    /// For a CLEAN document: reference to the corresponding TRACKED version (if it exists).
    /// For a TRACKED document: reference to the corresponding CLEAN version(if it exists).
    /// </summary>
    public Guid? LinkedDocumentId { get; set; }

    /// <summary>
    /// Name of the file to replace.
    /// </summary>
    public string LinkedDocumentFileName { get; set; } = string.Empty;

    /// <summary>
    /// Path to where the document is stored (e.g., in blob storage or file system).
    /// </summary>
    public string? LinkedDocumentStoragePath { get; set; }

    public List<GdsOption> DocumentToReplaceList { get; set; } = [];

    public IFormFile File { get; set; }

    public bool ShowSupersedeDocumentSection { get; set; }

    /// <summary>
    /// Gets the display size in KB, MB or GB, rounded to 2 decimal places.
    /// </summary>
    public string DisplaySize
    {
        get
        {
            const double KB = 1024;
            const double MB = KB * 1024;
            const double GB = MB * 1024;

            if (FileSize >= GB)
                return $"{Math.Round(FileSize / GB, 2)} GB";
            else if (FileSize >= MB)
                return $"{Math.Round(FileSize / MB, 2)} MB";
            else
                return $"{Math.Round(FileSize / KB, 2)} KB";
        }
    }
}