namespace Rsp.Portal.Application.DTOs;

/// <summary>
/// Represents a summary view of a document, including metadata such as file name, URI, and size.
/// </summary>
public class DocumentSummaryItemDto
{
    /// <summary>
    /// Unique identifier for the uploaded document.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the name of the uploaded file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public or internal URI to access the blob/file in storage.
    /// </summary>
    public string BlobUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the status of the document details.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets if the scan is successful or not.
    /// </summary>
    public bool? IsMalwareScanSuccessful { get; set; }

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