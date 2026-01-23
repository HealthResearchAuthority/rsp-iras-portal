using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Areas.Admin.Models;

public class UserRoleViewModel : RoleViewModel
{
    public bool IsSelected { get; set; }
    public string? DisplayName => Name?.Replace("_", " ")?.ToSentenceCase();
}