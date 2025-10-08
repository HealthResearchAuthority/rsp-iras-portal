namespace Rsp.IrasPortal.Web.Models;

public class SponsorOrganisationModel
{
    public Guid Id { get; set; }
    public List<string> Countries { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}