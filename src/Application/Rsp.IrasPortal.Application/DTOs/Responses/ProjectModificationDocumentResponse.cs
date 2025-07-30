namespace Rsp.IrasPortal.Application.DTOs.Responses;

/// <summary>
/// Represents a response model for a document associated with a project modification change.
/// </summary>
public class ProjectModificationDocumentResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the document.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated project modification change.
    /// </summary>
    public Guid ProjectModificationChangeId { get; set; }

    /// <summary>
    /// Gets or sets the name of the uploaded file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display size of the file (e.g., "2.1 MB").
    /// </summary>
    public string FileSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the document type, if specified.
    /// </summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the version of the document as provided by the sponsor.
    /// </summary>
    public string? SponsorDocumentVersion { get; set; }

    /// <summary>
    /// Gets or sets the path to the file in blob/document storage.
    /// </summary>
    public string? DocumentStoragePath { get; set; }

    /// <summary>
    /// Gets or sets the day component of the sponsor document date.
    /// </summary>
    public int? SponsorDocumentDateDay { get; set; }

    /// <summary>
    /// Gets or sets the month component of the sponsor document date.
    /// </summary>
    public int? SponsorDocumentDateMonth { get; set; }

    /// <summary>
    /// Gets or sets the year component of the sponsor document date.
    /// </summary>
    public int? SponsorDocumentDateYear { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this document has a previously approved version.
    /// </summary>
    public bool? HasPreviousVersionApproved { get; set; }
}