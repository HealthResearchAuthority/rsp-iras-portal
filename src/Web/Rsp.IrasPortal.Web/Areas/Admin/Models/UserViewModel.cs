using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserViewModel
{
    [HiddenInput]
    public string? Id { get; set; }

    [HiddenInput]
    public string? OriginalEmail { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = null!;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    public void Deconstruct(out string firstName, out string lastName, out string email)
    {
        firstName = FirstName;
        lastName = LastName;
        email = Email;
    }

    public void Deconstruct(out string id, out string originalEmail, out string firstName, out string lastName, out string email)
    {
        id = Id!;
        originalEmail = OriginalEmail!;
        firstName = FirstName;
        lastName = LastName;
        email = Email;
    }
}