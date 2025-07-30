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
    public Guid ProjectModificationChangeId { get; set; }

    /// <summary>
    /// Gets or sets the project record identifier.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the project personnel identifier.
    /// </summary>
    public string ProjectPersonnelId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the related to document type.
    /// </summary>
    public Guid DocumentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the document file name.
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document storage path.
    /// </summary>
    public string? DocumentStoragePath { get; set; }

    /// <summary>
    /// Gets or sets the document file size.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Gets or sets the document version.
    /// </summary>
    public string? SponsorDocumentVersion { get; set; }

    /// <summary>
    /// Gets or sets if the document has previous version.
    /// </summary>
    public bool? HasPreviousVersion { get; set; }

    /// <summary>
    /// Gets or sets the sponsor document date.
    /// </summary>
    public DateTime? SponsorDocumentDate { get; set; }
}