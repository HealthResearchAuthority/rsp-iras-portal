namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

/// <summary>
/// Pagination view model.
/// </summary>
public class PaginationViewModel
{
    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the route name.
    /// </summary>
    public string RouteName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the field to sort by if available.
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// Gets or sets the sort direction if available (e.g., "asc" or "desc").
    /// </summary>
    public string? SortDirection { get; set; }

    /// <summary>
    /// Gets or sets the search query if available
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets a complex object for advanced filtering.
    /// </summary>
    public object? ComplexSearchQuery { get; set; }

    /// <summary>
    /// Gets or sets any additional parameters that should be part of the pagination URL
    /// </summary>
    public IDictionary<string, string> AdditionalParameters { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Pages available in this pagination
    /// </summary>
    public List<int?> Pages { get; } = [];

    public int TotalPages { get; }

    public PaginationViewModel(int pageNumber, int pageSize, int totalCount)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;

        var currentPage = PageNumber;
        var pageCount = (int)Math.Ceiling((double)TotalCount / PageSize);

        TotalPages = pageCount;

        if (pageCount <= 7)
        {
            for (int i = 1; i <= pageCount; i++)
            {
                Pages.Add(i);
            }
        }
        else
        {
            Pages.Add(1);

            if (currentPage > 3)
            {
                Pages.Add(null); // Add Ellipsis
            }

            for (int i = currentPage - 1; i <= currentPage + 1; i++)
            {
                if (i > 1 && i < pageCount)
                {
                    Pages.Add(i);
                }
            }

            if (currentPage < pageCount - 2)
            {
                Pages.Add(null); // Add Ellipsis
            }

            Pages.Add(pageCount);
        }
    }
}