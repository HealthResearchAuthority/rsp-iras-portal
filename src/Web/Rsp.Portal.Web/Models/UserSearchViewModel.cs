using System.Diagnostics.CodeAnalysis;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

[ExcludeFromCodeCoverage]
public class UserSearchViewModel
{
    public UserSearchModel Search { get; set; } = new();
    public IEnumerable<UserViewModel> Users { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }


}