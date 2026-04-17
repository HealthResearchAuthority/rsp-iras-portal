namespace Rsp.Portal.Application.DTOs;

public class ReviewBodyUserDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public DateTime DateAdded { get; set; }
    public string? Telephone { get; set; }
    public string? CommitteeRole { get; set; }
    public string? Designation { get; set; }
    public bool MemberLeftOrganisation { get; set; }
    public DateTime? DateTimeLeft { get; set; }
    public DateTime? DateTimeLastUpdated { get; set; }
}