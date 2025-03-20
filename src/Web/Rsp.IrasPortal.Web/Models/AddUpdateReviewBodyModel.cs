namespace Rsp.IrasPortal.Web.Models;

public class AddUpdateReviewBodyModel
{
    public Guid Id { get; set; }
    public string OrganisationName { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public string? Description { get; set; }
    public List<string> Countries { get; set; } 
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; } 
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedDate { get; set; }
}