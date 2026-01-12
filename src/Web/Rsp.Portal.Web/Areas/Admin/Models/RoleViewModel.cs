using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.Portal.Web.Areas.Admin.Models;

public class RoleViewModel
{
    [HiddenInput]
    public string? Id { get; set; }

    [HiddenInput]
    public string? OriginalName { get; set; }

    [Required]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = null!;
}