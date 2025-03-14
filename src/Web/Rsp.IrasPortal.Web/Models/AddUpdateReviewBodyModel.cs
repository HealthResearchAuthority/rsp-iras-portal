using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Web.Models;

public class AddUpdateReviewBodyModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Enter the organisation name")]
    public string OrganisationName { get; set; }
    [Required(ErrorMessage = "Enter an email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string EmailAddress { get; set; }
    public string Description { get; set; }
    public List<string> Countries { get; set; } = new();
    public bool IsActive { get; set; } = true;
    //public string CreatedBy { get; set; }
    //public string UpdatedBy { get; set; }
}
