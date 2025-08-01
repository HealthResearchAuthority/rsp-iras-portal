using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel for capturing details of a document being added during a modification process.
/// </summary>
public class ModificationAddDocumentDetailsViewModel
{
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
    /// Optional version label provided by the sponsor for the document.
    /// </summary>
    public string? SponsorDocumentVersion { get; set; }

    /// <summary>
    /// Path to where the document is stored (e.g., in blob storage or file system).
    /// </summary>
    public string? DocumentStoragePath { get; set; }

    /// <summary>
    /// Day part of the sponsor-provided document date.
    /// </summary>
    public int? SponsorDocumentDateDay { get; set; }

    /// <summary>
    /// Month part of the sponsor-provided document date.
    /// </summary>
    public int? SponsorDocumentDateMonth { get; set; }

    /// <summary>
    /// Year part of the sponsor-provided document date.
    /// </summary>
    public int? SponsorDocumentDateYear { get; set; }

    /// <summary>
    /// Indicates whether a previous version of the document has been approved.
    /// </summary>
    public bool? HasPreviousVersionApproved { get; set; }

    /// <summary>
    /// Dropdown list of months (1–12) with full month names, based on the current culture.
    /// </summary>
    public List<SelectListItem> MonthOptions => Enumerable.Range(1, 12)
        .Select(m => new SelectListItem
        {
            Value = m.ToString(),
            Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
        }).ToList();
}