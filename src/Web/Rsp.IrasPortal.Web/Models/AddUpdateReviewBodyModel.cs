using System.ComponentModel.DataAnnotations;
using Rsp.IrasPortal.Web.Attributes;

namespace Rsp.IrasPortal.Web.Models;

public class AddUpdateReviewBodyModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Enter the organisation name")]
    public string OrganisationName { get; set; } = null!;

    [Required(ErrorMessage = "Enter an email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string EmailAddress { get; set; } = null!;

    [MaxWords(250, ErrorMessage = "The description cannot exceed 250 words.")]
    public string? Description { get; set; }

    [RequiredList(ErrorMessage = "Select at least one country.")]
    public List<string> Countries { get; set; } = [];

    public bool IsActive { get; set; } = true;
}