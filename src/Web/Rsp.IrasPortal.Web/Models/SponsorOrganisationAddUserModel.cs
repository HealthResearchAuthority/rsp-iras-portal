namespace Rsp.Portal.Web.Models;

public class SponsorOrganisationAddUserModel
{
    public string RtsId { get; set; } = null!;
    public Guid UserId { get; set; }
    public string? SponsorRole { get; set; }
    public bool IsAuthoriser { get; set; }
}