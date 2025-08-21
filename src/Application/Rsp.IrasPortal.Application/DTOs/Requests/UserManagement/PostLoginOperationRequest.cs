namespace Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;

public class PostLoginOperationRequest
{
    public string? UserEmail { get; set; }
    public string? Telephone { get; set; }
    public string? IdentityProviderId { get; set; }
}