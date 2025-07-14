using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class UserSearchViewModel
{
    public UserSearchModel Search { get; set; } = new();
    public IEnumerable<UserViewModel> Users { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}