using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.ValidationAttributes;

namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserViewModel
{
    [HiddenInput]
    public string? Id { get; set; }

    [HiddenInput]
    public string? OriginalEmail { get; set; }

    [Display(Name = "Title")]
    [MaxLength(250, ErrorMessage = "Field cannot contain more than 250 characters.")]
    public string? Title { get; set; } = null;

    [Required]
    [StringLength(250, ErrorMessage = "Field cannot contain more than 250 characters.")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(250, ErrorMessage = "Field cannot contain more than 250 characters.")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Display(Name = "Telephone")]
    [Phone(ErrorMessage = "Telephone number not in corret format.")]
    [MaxLength(11, ErrorMessage = "Field cannot contain more than 11 characters.")]
    public string? Telephone { get; set; } = null!;

    [Display(Name = "Organisation")]
    [MaxLength(250, ErrorMessage = "Field cannot contain more than 250 characters.")]
    public string? Organisation { get; set; } = null;

    [Display(Name = "JobTitle")]
    [MaxLength(250, ErrorMessage = "Field cannot contain more than 250 characters.")]
    public string? JobTitle { get; set; } = null;

    [Display(Name = "Role")]
    public string? Role { get; set; } = null!;

    [Display(Name = "Country")]
    [RequiredIfTrue("Role", "operations", ErrorMessage = "Field is mandatory when the role 'operations' is selected.")]
    public IList<string>? Country { get; set; } = null;

    [Display(Name = "Access Required")]
    [RequiredIfTrue("Role", "operations", ErrorMessage = "Field is mandatory when the role 'operations' is selected.")]
    public IList<string>? AccessRequired { get; set; } = null;

    [Display(Name = "Last updated")]
    public DateTime? LastUpdated { get; set; } = null;

    public IList<Role> AvailableUserRoles { get; set; } = new List<Role>();

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