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