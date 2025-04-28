namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

/// <summary>
/// Pagination view model.
/// </summary>
public class PaginationViewModel
{
    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the route name.
    /// </summary>
    public string RouteName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total count.
    /// </summary>s
    public int TotalCount { get; set; }

    public string? ReviewBodyId { get; set; } = null;

    public string? SearchQuery { get; set; }
}