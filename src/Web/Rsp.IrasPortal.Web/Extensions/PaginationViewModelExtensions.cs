using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Extensions;

public static class PaginationViewModelExtensions
{
    /// <summary>
    /// Builds a PaginationViewModel with optional sorting and additional route parameters.
    /// </summary>
    public static PaginationViewModel BuildPagination(
        int pageNumber,
        int pageSize,
        int totalCount,
        string routeName,
        string? sortField = null,
        string? sortDirection = null,
        IDictionary<string, string>? extra = null)
    {
        var pagination = new PaginationViewModel(pageNumber, pageSize, totalCount)
        {
            RouteName = routeName,
            SortField = sortField,
            SortDirection = sortDirection
        };

        if (extra is not null)
        {
            foreach (var (key, value) in extra)
            {
                pagination.AdditionalParameters[key] = value;
            }
        }

        return pagination;
    }
}