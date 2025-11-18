namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class ProjectModificationDocumentRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the modification.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the identifier of the related project modification change.
    /// </summary>
    public Guid ProjectModificationId { get; set; }

    /// <summary>
    /// Gets or sets the project record identifier.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the project personnel identifier.
    /// </summary>
    public string ProjectPersonnelId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document file name.
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document storage path.
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
    /// Gets or sets the document file size in bytes.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Gets or sets the date the document was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets the display size in KB, MB or GB, rounded to 2 decimal places.
    /// Returns "-" if size is null or zero.
    /// </summary>
    public string DisplaySize
    {
        get
        {
            if (FileSize == null || FileSize == 0)
                return "-";

            const double KB = 1024;
            const double MB = KB * 1024;
            const double GB = MB * 1024;

            double size = FileSize.Value;

            if (size >= GB)
                return $"{Math.Round(size / GB, 2)} GB";
            else if (size >= MB)
                return $"{Math.Round(size / MB, 2)} MB";
            else
                return $"{Math.Round(size / KB, 2)} KB";
        }
    }
}