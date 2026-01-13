namespace Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

public class SponsorMyOrganisationUserViewModel
{
    public string UserId { get; set; } = null!;
    public string SponsorOrganisationUserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string GivenName { get; set; } = null!;
    public string FamilyName { get; set; } = null!;
    public string? Title { get; set; }
    public string? Telephone { get; set; }
    public string? Organisation { get; set; }
    public string? JobTitle { get; set; }
    public string? Role { get; set; }
    public string IsAuthoriser { get; set; } = null!;
    public string? Status { get; set; }
    public string RtsId { get; set; } = null!;
    public string SponsorOrganisationName { get; set; } = null!;
    public bool IsLoggedInUserAdmin { get; set; } = false;
}