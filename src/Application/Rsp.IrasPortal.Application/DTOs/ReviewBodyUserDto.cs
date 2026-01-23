namespace Rsp.Portal.Application.DTOs;

public class ReviewBodyUserDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public DateTime DateAdded { get; set; }
}