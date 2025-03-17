namespace Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;

public class UpdateUserRequest : CreateUserRequest
{
    public string OriginalEmail { get; set; } = null!;
}