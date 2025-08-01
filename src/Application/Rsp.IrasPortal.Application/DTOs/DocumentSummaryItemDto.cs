namespace Rsp.IrasPortal.Application.DTOs;

/// <summary>
/// Represents a summary view of a document, including metadata such as file name, URI, and size.
/// </summary>
public class DocumentSummaryItemDto
{
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
    /// Gets the display size in megabytes (MB), rounded to 2 decimal places.
    /// </summary>
    public string DisplaySize => $"{Math.Round((double)FileSize / (1024 * 1024), 2)} MB";

    /// <summary>
    /// Gets or sets the status of the document details.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}