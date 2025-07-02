namespace Rsp.IrasPortal.Web.Models;

public class AddUpdateReviewBodyModel
{
    public Guid Id { get; set; }
    public string? RegulatoryBodyName { get; set; }
    public string? EmailAddress { get; set; }
    public string? Description { get; set; }
    public List<string> Countries { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}