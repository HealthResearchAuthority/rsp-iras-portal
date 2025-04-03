namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserRoleViewModel : RoleViewModel
{
    public bool IsSelected { get; set; } = true;
    public string RoleName { get; set; }
}