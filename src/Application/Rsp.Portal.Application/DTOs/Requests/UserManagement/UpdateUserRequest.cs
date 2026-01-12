namespace Rsp.Portal.Application.DTOs.Requests.UserManagement;

public class UpdateUserRequest : CreateUserRequest
{
    public string OriginalEmail { get; set; } = null!;
    public DateTime? CurrentLogin { get; set; } = null;
}