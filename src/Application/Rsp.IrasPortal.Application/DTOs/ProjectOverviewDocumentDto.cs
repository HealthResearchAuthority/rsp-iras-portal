namespace Rsp.IrasPortal.Application.DTOs;

public class ProjectOverviewDocumentDto
{
    /// <summary>
    /// Gets or sets the name of the uploaded file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path where the document is stored in blob storage.
    /// </summary>
    public string? DocumentStoragePath { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the document type, if specified.
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the document version, if specified.
    /// </summary>
    public string DocumentVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the document date, if specified.
    /// </summary>
    public DateTime? DocumentDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the status, if specified.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the modification, if specified.
    /// </summary>
    public string ModificationIdentifier { get; set; } = string.Empty;
}