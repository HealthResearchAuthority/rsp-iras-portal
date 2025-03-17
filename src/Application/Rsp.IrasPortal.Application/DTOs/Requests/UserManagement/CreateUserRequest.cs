namespace Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;

public class CreateUserRequest
{
    public string? Title { get; set; } = null;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? JobTitle { get; set; } = null;
    public string? Organisation { get; set; } = null;
    public string? Telephone { get; set; } = null;
    public string? Country { get; set; } = null;
    public string Status { get; set; } = null!;
    public DateTime? LastUpdated { get; set; } = null;
}