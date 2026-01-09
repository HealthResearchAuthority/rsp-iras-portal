namespace Rsp.IrasPortal.Web.Models;

public class SponsorOrganisationAddUserModel
{
    public string RtsId { get; set; } = null!;
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public bool IsAuthoriser { get; set; }
}