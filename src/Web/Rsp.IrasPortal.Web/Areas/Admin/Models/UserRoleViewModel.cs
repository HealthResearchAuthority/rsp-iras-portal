﻿using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserRoleViewModel : RoleViewModel
{
    public bool IsSelected { get; set; }
    public string? DisplayName => Name?.Replace("_", " ")?.ToSentenceCase();
}