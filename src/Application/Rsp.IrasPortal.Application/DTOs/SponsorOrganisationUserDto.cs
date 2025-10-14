namespace Rsp.IrasPortal.Application.DTOs;

public class SponsorOrganisationUserDto
{
    public Guid Id { get; set; }
    public string RtsId { get; set; }
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public DateTime DateAdded { get; set; }
}