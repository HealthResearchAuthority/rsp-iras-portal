namespace Rsp.IrasPortal.Web.Models;

public class SponsorOrganisationModel
{
    public Guid Id { get; set; }
    public string RtsId { get; set; } = null!;
    public string SponsorOrganisationName { get; set; } = null!;
    public List<string> Countries { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}