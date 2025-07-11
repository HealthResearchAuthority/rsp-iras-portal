using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class ReviewBodySearchViewModel
{
    public ReviewBodySearchModel Search { get; set; } = new();
    public IEnumerable<ReviewBodyDto> ReviewBodies { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}