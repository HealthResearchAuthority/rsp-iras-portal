using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class ApplicationsViewModel
{
    public IEnumerable<ApplicationModel> Applications { get; set; } = new List<ApplicationModel>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public ApplicationSearchModel Search { get; set; } = new();
    public PaginationViewModel? Pagination { get; set; }
    public bool EmptySearchPerformed { get; set; } = false;
}