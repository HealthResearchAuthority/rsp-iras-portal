using System.Diagnostics.CodeAnalysis;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

[ExcludeFromCodeCoverage]
public class ReviewBodySearchViewModel
{
    public ReviewBodySearchModel Search { get; set; } = new();
    public IEnumerable<ReviewBodyDto> ReviewBodies { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}