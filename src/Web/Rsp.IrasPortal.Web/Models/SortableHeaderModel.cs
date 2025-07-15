using System.Diagnostics.CodeAnalysis;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class SortableHeaderModel
{
    public string FieldName { get; set; } = null!;
    public string DisplayText { get; set; } = null!;
    public string? CurrentSortField { get; set; }
    public string? CurrentSortDirection { get; set; }
    public string? FormAction { get; set; }
    public string? TableId { get; set; }
}