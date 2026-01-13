namespace Rsp.Portal.Web.Areas.Admin.Models;

public class UserRolesViewModel
{
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IList<UserRoleViewModel> UserRoles { get; set; } = [];
}