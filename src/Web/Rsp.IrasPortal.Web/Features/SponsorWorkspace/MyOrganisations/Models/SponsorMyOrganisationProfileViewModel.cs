namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationProfileViewModel
{
    public string RtsId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string Address { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
}