namespace Rsp.Portal.Application.DTOs;

public class SponsorOrganisationUserDto
{
    public Guid Id { get; set; }
    public string RtsId { get; set; }
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public DateTime DateAdded { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAuthoriser { get; set; }
    public string SponsorRole { get; set; } = null!;
}