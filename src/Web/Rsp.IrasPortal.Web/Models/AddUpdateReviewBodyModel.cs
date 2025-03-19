namespace Rsp.IrasPortal.Web.Models;

public class AddUpdateReviewBodyModel
{
    public Guid Id { get; set; }
    public string OrganisationName { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public string? Description { get; set; }
    public List<string> Countries { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedDate { get; set; }
}