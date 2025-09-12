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
}