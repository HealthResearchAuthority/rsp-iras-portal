using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rsp.IrasPortal.Web.Models;

public class ModificationAddDocumentDetailsViewModel
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;

    public int? DocumentTypeId { get; set; }
    public List<SelectListItem> DocumentTypeOptions { get; set; } = [];

    public string? SponsorDocumentVersion { get; set; }

    public int? SponsorDocumentDateDay { get; set; }
    public int? SponsorDocumentDateMonth { get; set; }
    public int? SponsorDocumentDateYear { get; set; }

    public bool? HasPreviousVersionApproved { get; set; }

    public List<SelectListItem> MonthOptions => Enumerable.Range(1, 12)
        .Select(m => new SelectListItem
        {
            Value = m.ToString(),
            Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
        }).ToList();
}