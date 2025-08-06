using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ApplicationsViewModel
{
    public IEnumerable<ApplicationModel> Applications { get; set; } = new List<ApplicationModel>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public ApplicationSearchModel Search { get; set; } = new();
    public PaginationViewModel? Pagination { get; set; }
}