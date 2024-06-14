using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserRoleViewModel : RoleViewModel
{
    public bool IsSelected { get; set; }
}